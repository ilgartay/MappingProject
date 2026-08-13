import { useCallback, useEffect, useRef, useState } from 'react'
import Map from 'ol/Map'
import View from 'ol/View'
import TileLayer from 'ol/layer/Tile'
import VectorLayer from 'ol/layer/Vector'
import VectorSource from 'ol/source/Vector'
import OSM from 'ol/source/OSM'
import Draw from 'ol/interaction/Draw'
import Feature from 'ol/Feature'
import Point from 'ol/geom/Point'
import { fromLonLat, transformExtent } from 'ol/proj'
import 'ol/ol.css'

import { ENDPOINT_BY_GEOMETRY, createFeature, deleteFeature, fetchFeatures } from '../api/features'
import { geometryToWkt, wktToFeature } from '../map/wkt'
import { featureStyle, targetStyle } from '../map/styles'
import CoordinateSearch from './CoordinateSearch'
import DrawToolbar from './DrawToolbar'
import SaveFeatureDialog from './SaveFeatureDialog'
import DeleteFeatureDialog from './DeleteFeatureDialog'
import './MapView.css'

// Türkiye'nin yaklaşık sınır kutusu: [batı, güney, doğu, kuzey] (derece)
const TURKEY_EXTENT = [25.5, 35.7, 45.0, 42.3]
const TURKEY_CENTER = [35.2, 39.0]

/**
 * Feature id'lerini "points-3" biçiminde kuruyoruz; buradan hem silme
 * uç noktasını hem de veritabanı id'sini geri çıkarıyoruz.
 * Henüz kaydedilmemiş çizimlerin id'si olmaz, o durumda null döner.
 */
function parseFeatureId(feature) {
  const raw = feature.getId()
  if (!raw) return null

  const [group, id] = String(raw).split('-')
  return {
    countKey: group, // 'points' | 'lines' | 'polygons'
    endpoint: group.slice(0, -1), // 'point' | 'line' | 'polygon'
    id: Number(id),
  }
}

export default function MapView() {
  const containerRef = useRef(null)
  const mapRef = useRef(null)
  const sourceRef = useRef(null)
  const featureLayerRef = useRef(null)
  const targetSourceRef = useRef(null)
  const drawRef = useRef(null)

  const [activeTool, setActiveTool] = useState(null) // 'Point' | 'LineString' | 'Polygon'
  const [pending, setPending] = useState(null) // kaydedilmeyi bekleyen çizim
  const [selected, setSelected] = useState(null) // silinmek üzere seçilen çizim
  const [counts, setCounts] = useState({ points: 0, lines: 0, polygons: 0 })
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  // --- Haritayı bir kez kur ---
  useEffect(() => {
    const source = new VectorSource()
    sourceRef.current = source

    // Aranan koordinatın işareti ayrı katmanda dursun: çizimlerle karışmasın,
    // tıklayarak silme akışına da takılmasın.
    const targetSource = new VectorSource()
    targetSourceRef.current = targetSource

    const featureLayer = new VectorLayer({ source, style: featureStyle })
    featureLayerRef.current = featureLayer

    const map = new Map({
      target: containerRef.current,
      layers: [
        new TileLayer({ source: new OSM() }),
        // Çizimler altlık haritanın üstündeki bu vektör katmanında yaşıyor.
        featureLayer,
        new VectorLayer({ source: targetSource, style: targetStyle }),
      ],
      view: new View({
        center: fromLonLat(TURKEY_CENTER),
        zoom: 6,
        minZoom: 3,
      }),
    })

    mapRef.current = map

    // Sabit zoom yerine extent'e oturtuyoruz: fit() ekran boyutunu bildiği
    // için Türkiye her cihazda tam görünür. getSize() ilk çizimden önce
    // undefined olduğundan postrender'ı bekliyoruz.
    map.once('postrender', () => {
      const size = map.getSize()
      if (!size) return

      map.getView().fit(transformExtent(TURKEY_EXTENT, 'EPSG:4326', 'EPSG:3857'), {
        size,
        padding: [24, 24, 24, 24],
        maxZoom: 9,
      })
    })

    return () => map.setTarget(undefined)
  }, [])

  // --- Kayıtlı geometrileri yükle ---
  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const data = await fetchFeatures()
        if (cancelled) return

        const source = sourceRef.current
        source.clear()

        for (const [key, group] of Object.entries(data)) {
          for (const item of group) {
            // WKT 4326 -> harita 3857 dönüşümü burada oluyor.
            const feature = wktToFeature(item.wkt)
            feature.set('name', item.name)
            feature.setId(`${key}-${item.id}`)
            source.addFeature(feature)
          }
        }

        setCounts({
          points: data.points.length,
          lines: data.lines.length,
          polygons: data.polygons.length,
        })
      } catch (err) {
        // 401 ise interceptor zaten oturumu kapatıp login'e atıyor.
        if (!cancelled && err.response?.status !== 401) {
          setError('Kayıtlı çizimler yüklenemedi.')
        }
      } finally {
        if (!cancelled) setIsLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [])

  // --- Seçili araca göre Draw etkileşimini kur ---
  useEffect(() => {
    const map = mapRef.current
    if (!map) return

    // Önceki aracı her durumda kaldır: iki etkileşim aynı anda açık kalırsa
    // tek tıklama iki geometri çizmeye başlar.
    if (drawRef.current) {
      map.removeInteraction(drawRef.current)
      drawRef.current = null
    }

    // Kaydet penceresi açıkken yeni çizim başlatılmasın.
    if (!activeTool || pending) return

    const draw = new Draw({ source: sourceRef.current, type: activeTool })

    draw.on('drawend', (event) => {
      // Feature'ı source'a Draw kendisi ekliyor; biz sadece adını sormak
      // için bekletiyoruz. İptal edilirse geri çıkaracağız.
      setPending({ feature: event.feature, geometryType: activeTool })
    })

    map.addInteraction(draw)
    drawRef.current = draw

    return () => {
      map.removeInteraction(draw)
      drawRef.current = null
    }
  }, [activeTool, pending])

  // --- Çizime tıklayınca silme penceresini aç ---
  useEffect(() => {
    const map = mapRef.current
    if (!map) return

    // Çizim yapılırken veya bir pencere açıkken seçim devre dışı;
    // yoksa çizim tıklaması aynı anda silme penceresini de açardı.
    if (activeTool || pending || selected) return

    function onClick(event) {
      // layerFilter: sadece çizim katmanına bak. Olmasaydı arama işaretine
      // tıklamak üstündeki çizimi bulmayı engellerdi.
      const feature = map.forEachFeatureAtPixel(event.pixel, (f) => f, {
        layerFilter: (layer) => layer === featureLayerRef.current,
      })
      if (!feature) return

      const parsed = parseFeatureId(feature)
      if (!parsed) return

      setSelected({
        ...parsed,
        feature,
        name: feature.get('name'),
        geometryType: feature.getGeometry().getType(),
      })
    }

    // Üzerine gelince imleç değişsin: tıklanabilir olduğu belli olsun.
    // İmleci kendi div'imize yazıyoruz; map.getTargetElement() harita
    // kapatıldıktan sonra undefined dönüyor ve temizlikte patlıyor.
    const element = containerRef.current

    function onPointerMove(event) {
      if (event.dragging) return
      const overFeature = map.hasFeatureAtPixel(event.pixel, {
        layerFilter: (layer) => layer === featureLayerRef.current,
      })
      element.style.cursor = overFeature ? 'pointer' : ''
    }

    map.on('click', onClick)
    map.on('pointermove', onPointerMove)

    return () => {
      map.un('click', onClick)
      map.un('pointermove', onPointerMove)
      element.style.cursor = ''
    }
  }, [activeTool, pending, selected])

  // --- Çizim modunda imleci artı yap ---
  useEffect(() => {
    const element = containerRef.current
    if (!element || !activeTool) return

    element.style.cursor = 'crosshair'

    return () => {
      element.style.cursor = ''
    }
  }, [activeTool])

  // --- Esc: devam eden çizimi ya da açık pencereyi iptal et ---
  useEffect(() => {
    function onKeyDown(event) {
      if (event.key !== 'Escape') return

      if (drawRef.current) {
        drawRef.current.abortDrawing()
      }
      setSelected(null)
      setActiveTool(null)
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [])

  const handleCancel = useCallback(() => {
    if (pending) {
      // Kaydedilmeyen çizim haritada kalmasın.
      sourceRef.current.removeFeature(pending.feature)
    }
    setPending(null)
  }, [pending])

  const handleSave = useCallback(
    async (name) => {
      const { feature, geometryType } = pending
      const endpoint = ENDPOINT_BY_GEOMETRY[geometryType]

      // Harita 3857 -> veritabanı 4326 dönüşümü burada oluyor.
      const wkt = geometryToWkt(feature.getGeometry())
      const saved = await createFeature(endpoint, { name, wkt })

      feature.set('name', saved.name)
      feature.setId(`${endpoint}s-${saved.id}`)

      setCounts((prev) => ({
        ...prev,
        points: prev.points + (geometryType === 'Point' ? 1 : 0),
        lines: prev.lines + (geometryType === 'LineString' ? 1 : 0),
        polygons: prev.polygons + (geometryType === 'Polygon' ? 1 : 0),
      }))
      setPending(null)
    },
    [pending],
  )

  /**
   * Girilen enlem/boylama uçar ve orayı işaretler.
   * Girdi EPSG:4326; fromLonLat haritanın EPSG:3857 sistemine çeviriyor.
   */
  const handleSearch = useCallback((longitude, latitude) => {
    const center = fromLonLat([longitude, latitude])

    targetSourceRef.current.clear()
    targetSourceRef.current.addFeature(new Feature(new Point(center)))

    // Anında ışınlanmak yerine animasyon: kullanıcı haritanın nereden
    // nereye gittiğini takip edebilsin.
    mapRef.current.getView().animate({ center, zoom: 12, duration: 800 })
  }, [])

  const handleDelete = useCallback(async () => {
    const { feature, endpoint, id, countKey } = selected

    await deleteFeature(endpoint, id)

    sourceRef.current.removeFeature(feature)
    setCounts((prev) => ({ ...prev, [countKey]: Math.max(0, prev[countKey] - 1) }))
    setSelected(null)
  }, [selected])

  return (
    <div className="map-view">
      <div className="map-view__canvas" ref={containerRef} />

      <CoordinateSearch onSearch={handleSearch} />

      <DrawToolbar
        activeTool={activeTool}
        onSelect={setActiveTool}
        counts={counts}
        disabled={pending !== null || isLoading}
      />

      {isLoading && (
        <p className="map-view__loading" role="status">
          Kayıtlı çizimler yükleniyor…
        </p>
      )}

      {error && (
        <p className="map-view__error" role="alert">
          {error}
        </p>
      )}

      {pending && (
        <SaveFeatureDialog
          geometryType={pending.geometryType}
          onSave={handleSave}
          onCancel={handleCancel}
        />
      )}

      {selected && (
        <DeleteFeatureDialog
          name={selected.name}
          geometryType={selected.geometryType}
          onDelete={handleDelete}
          onCancel={() => setSelected(null)}
        />
      )}
    </div>
  )
}
