import { useCallback, useEffect, useRef, useState } from 'react'
import Map from 'ol/Map'
import View from 'ol/View'
import TileLayer from 'ol/layer/Tile'
import VectorLayer from 'ol/layer/Vector'
import VectorSource from 'ol/source/Vector'
import OSM from 'ol/source/OSM'
import Draw from 'ol/interaction/Draw'
import Modify from 'ol/interaction/Modify'
import Collection from 'ol/Collection'
import Feature from 'ol/Feature'
import Point from 'ol/geom/Point'
import { fromLonLat, transformExtent } from 'ol/proj'
import 'ol/ol.css'

import {
  ENDPOINT_BY_GEOMETRY,
  createFeature,
  deleteFeature,
  fetchFeatures,
  updateFeature,
} from '../api/features'
import { analyzeIntersection } from '../api/analysis'
import { geometryToWkt, wktToFeature } from '../map/wkt'
import { analysisStyle, featureStyle, targetStyle } from '../map/styles'
import { TURKEY_CENTER, TURKEY_EXTENT } from '../map/turkey'
import AnalysisPanel from './AnalysisPanel'
import FeatureDetailPanel from './FeatureDetailPanel'
import CoordinateSearch from './CoordinateSearch'
import DrawToolbar from './DrawToolbar'
import SaveFeatureDialog from './SaveFeatureDialog'
import DeleteFeatureDialog from './DeleteFeatureDialog'
import './MapView.css'

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
  const analysisSourceRef = useRef(null)
  const drawRef = useRef(null)

  // 'Point' | 'LineString' | 'Polygon' | 'Analysis'
  const [activeTool, setActiveTool] = useState(null)
  const [pending, setPending] = useState(null) // kaydedilmeyi bekleyen çizim
  const [selected, setSelected] = useState(null) // detayı açık olan çizim
  const [isConfirmingDelete, setIsConfirmingDelete] = useState(false)
  const [analysis, setAnalysis] = useState(null) // kesişim analizi sonucu
  const [counts, setCounts] = useState({ points: 0, lines: 0, polygons: 0 })
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  /**
   * Poligonu backend'e gönderip kesişen envanterleri sayar.
   * Hem kaydedilen poligon hem geçici analiz aracı bunu kullanıyor.
   * @param {string} wkt EPSG:4326 poligon
   * @param {string} title panelde gösterilecek başlık
   * @param {number} [excludePolygonId] poligonun kendisini saymamak için
   */
  const runAnalysis = useCallback(async (wkt, title, excludePolygonId) => {
    setAnalysis({ title, isLoading: true })

    try {
      const result = await analyzeIntersection(wkt, excludePolygonId)
      setAnalysis({ title, result })
    } catch (err) {
      // 401 ise interceptor zaten oturumu kapatıyor, panel göstermeye gerek yok.
      if (err.response?.status === 401) {
        setAnalysis(null)
        return
      }
      setAnalysis({ title, error: err.response?.data?.message ?? 'Analiz yapılamadı.' })
    }
  }, [])

  const clearAnalysis = useCallback(() => {
    analysisSourceRef.current?.clear()
    setAnalysis(null)
  }, [])

  // --- Haritayı bir kez kur ---
  useEffect(() => {
    const source = new VectorSource()
    sourceRef.current = source

    // Aranan koordinatın işareti ayrı katmanda dursun: çizimlerle karışmasın,
    // tıklayarak silme akışına da takılmasın.
    const targetSource = new VectorSource()
    targetSourceRef.current = targetSource

    // Geçici analiz poligonu da ayrı katmanda: veritabanına gitmiyor,
    // silme akışına takılmıyor, "Temizle" ile tek hamlede kalkıyor.
    const analysisSource = new VectorSource()
    analysisSourceRef.current = analysisSource

    const featureLayer = new VectorLayer({ source, style: featureStyle })
    featureLayerRef.current = featureLayer

    const map = new Map({
      target: containerRef.current,
      layers: [
        new TileLayer({ source: new OSM() }),
        // Çizimler altlık haritanın üstündeki bu vektör katmanında yaşıyor.
        featureLayer,
        new VectorLayer({ source: analysisSource, style: analysisStyle }),
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
            feature.set('color', item.color)
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

    // Analiz aracı da poligon çizer, ama başka katmana ve kaydetmeden.
    const isAnalysis = activeTool === 'Analysis'

    const draw = new Draw({
      source: isAnalysis ? analysisSourceRef.current : sourceRef.current,
      type: isAnalysis ? 'Polygon' : activeTool,
    })

    draw.on('drawend', (event) => {
      const wkt = geometryToWkt(event.feature.getGeometry())

      if (isAnalysis) {
        // Draw, yeni feature'ı bu olaydan SONRA source'a ekliyor;
        // o yüzden burada clear() demek sadece önceki analizi siler.
        analysisSourceRef.current.clear()

        // Aracı kapatıyoruz: sonuç paneli okunurken yanlışlıkla
        // yeni bir poligona başlanmasın.
        setActiveTool(null)
        runAnalysis(wkt, 'Envanter analizi')
        return
      }

      // Feature'ı source'a Draw kendisi ekliyor; biz sadece bilgilerini
      // sormak için bekletiyoruz. İptal edilirse geri çıkaracağız.
      setPending({ feature: event.feature, geometryType: activeTool })
    })

    map.addInteraction(draw)
    drawRef.current = draw

    return () => {
      map.removeInteraction(draw)
      drawRef.current = null
    }
  }, [activeTool, pending, runAnalysis])

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
        color: feature.get('color'),
        geometryType: feature.getGeometry().getType(),
        // "Vazgeç" geometriyi eski haline döndürebilsin diye kopya alıyoruz.
        originalGeometry: feature.getGeometry().clone(),
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

  // --- Seçili objenin geometrisini düzenlenebilir yap ---
  useEffect(() => {
    const map = mapRef.current
    if (!map || !selected) return

    // Collection'a sadece seçili feature'ı koyuyoruz: kullanıcı yanlışlıkla
    // komşu bir çizimin köşesini oynatamasın.
    const modify = new Modify({ features: new Collection([selected.feature]) })
    map.addInteraction(modify)

    return () => map.removeInteraction(modify)
  }, [selected])

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
      setActiveTool(null)
      setIsConfirmingDelete(false)
      // Geometri değişikliği varsa geri alınsın diye state'i fonksiyonel
      // güncelliyoruz; effect'in bağımlılığına selected eklemeye gerek kalmıyor.
      setSelected((current) => {
        current?.feature.setGeometry(current.originalGeometry)
        return null
      })
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
    async (name, color) => {
      const { feature, geometryType } = pending
      const endpoint = ENDPOINT_BY_GEOMETRY[geometryType]

      // Harita 3857 -> veritabanı 4326 dönüşümü burada oluyor.
      const wkt = geometryToWkt(feature.getGeometry())
      const saved = await createFeature(endpoint, { name, wkt, color })

      feature.set('name', saved.name)
      feature.set('color', saved.color)
      feature.setId(`${endpoint}s-${saved.id}`)

      setCounts((prev) => ({
        ...prev,
        points: prev.points + (geometryType === 'Point' ? 1 : 0),
        lines: prev.lines + (geometryType === 'LineString' ? 1 : 0),
        polygons: prev.polygons + (geometryType === 'Polygon' ? 1 : 0),
      }))
      setPending(null)

      // Poligon kaydedildiyse içinde kalan envanteri hemen say.
      // await etmiyoruz: pencere kapansın, sonuç panelde belirsin.
      // Kendisini saymaması için id'sini hariç tutuyoruz.
      if (geometryType === 'Polygon') {
        runAnalysis(wkt, 'Kaydedilen poligon analizi', saved.id)
      }
    },
    [pending, runAnalysis],
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

  /** Detay penceresindeki isim/renk ile haritada düzenlenen geometriyi birlikte kaydeder. */
  const handleUpdate = useCallback(
    async (name, color) => {
      const { feature, endpoint, id } = selected

      // Harita 3857 -> veritabanı 4326 dönüşümü burada oluyor.
      const wkt = geometryToWkt(feature.getGeometry())
      const saved = await updateFeature(endpoint, id, { name, wkt, color })

      feature.set('name', saved.name)
      feature.set('color', saved.color)
      setSelected(null)
    },
    [selected],
  )

  /** Vazgeç: harita üzerinde sürüklenen köşeleri eski haline döndürür. */
  const handleCancelEdit = useCallback(() => {
    if (selected) {
      selected.feature.setGeometry(selected.originalGeometry)
    }
    setSelected(null)
    setIsConfirmingDelete(false)
  }, [selected])

  /** Onaydan sonra soft delete: sunucu satırı silmiyor, is_deleted = true yapıyor. */
  const handleDelete = useCallback(async () => {
    const { feature, endpoint, id, countKey } = selected

    await deleteFeature(endpoint, id)

    sourceRef.current.removeFeature(feature)
    setCounts((prev) => ({ ...prev, [countKey]: Math.max(0, prev[countKey] - 1) }))
    setIsConfirmingDelete(false)
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

      {/* Detay paneli açıkken analiz paneli gizleniyor: ikisi de sağ üstte
          duruyor ve üst üste binince alttakinin düğmelerine erişilemiyor.
          Sonuç state'te kalıyor, detay kapanınca panel geri geliyor. */}
      {analysis && !selected && (
        <AnalysisPanel
          title={analysis.title}
          isLoading={analysis.isLoading}
          result={analysis.result}
          error={analysis.error}
          onClose={clearAnalysis}
        />
      )}

      {selected && (
        <FeatureDetailPanel
          feature={selected}
          onSave={handleUpdate}
          onCancel={handleCancelEdit}
          onDeleteRequest={() => setIsConfirmingDelete(true)}
        />
      )}

      {selected && isConfirmingDelete && (
        <DeleteFeatureDialog
          name={selected.name}
          geometryType={selected.geometryType}
          onDelete={handleDelete}
          onCancel={() => setIsConfirmingDelete(false)}
        />
      )}
    </div>
  )
}
