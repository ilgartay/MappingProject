import { useCallback, useEffect, useRef, useState } from 'react'
import Map from 'ol/Map'
import View from 'ol/View'
import TileLayer from 'ol/layer/Tile'
import VectorLayer from 'ol/layer/Vector'
import VectorSource from 'ol/source/Vector'
import OSM from 'ol/source/OSM'
import Draw from 'ol/interaction/Draw'
import { fromLonLat, transformExtent } from 'ol/proj'
import { Fill, Stroke, Style } from 'ol/style'
import 'ol/ol.css'

import { fetchGeoArea, saveGeoArea } from '../../api/admin'
import { geometryToWkt, wktToFeature } from '../../map/wkt'
import { TURKEY_CENTER, TURKEY_EXTENT } from '../../map/turkey'
import { TURKEY_REGIONS, regionsToWkt } from '../../map/regions'
import './GeoAreaDialog.css'

const AREA_STYLE = new Style({
  stroke: new Stroke({ color: '#009bff', width: 2.5 }),
  fill: new Fill({ color: 'rgba(0, 155, 255, 0.15)' }),
})

/**
 * Kullanıcı veya rol için çizim alanı tanımlar.
 * Harita Türkiye'ye zoomlu açılır; çizilen poligon o kişinin/rolün
 * çizim yapabileceği sınırı belirler.
 *
 * @param {{type: 'user'|'role', id: number, label: string}} target
 */
export default function GeoAreaDialog({ target, onClose }) {
  const containerRef = useRef(null)
  const sourceRef = useRef(null)

  const [name, setName] = useState('')
  const [hasArea, setHasArea] = useState(false)

  // Seçili bölge kimlikleri. Elle çizim yapılınca boşalıyor: bir alan ya
  // hazır bölgelerden ya da elle çizimden gelir, ikisi karışmaz.
  const [regionIds, setRegionIds] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState('')

  // --- Haritayı kur ---
  useEffect(() => {
    const source = new VectorSource()
    sourceRef.current = source

    const map = new Map({
      target: containerRef.current,
      layers: [
        new TileLayer({ source: new OSM() }),
        new VectorLayer({ source, style: AREA_STYLE }),
      ],
      view: new View({ center: fromLonLat(TURKEY_CENTER), zoom: 6, minZoom: 3 }),
    })

    // Türkiye'ye oturt; getSize() ilk çizimden önce undefined.
    map.once('postrender', () => {
      const size = map.getSize()
      if (!size) return

      map.getView().fit(transformExtent(TURKEY_EXTENT, 'EPSG:4326', 'EPSG:3857'), {
        size,
        padding: [20, 20, 20, 20],
        maxZoom: 9,
      })
    })

    // Tek alan tanımlıyoruz: yeni poligon eskisinin yerini alsın.
    const draw = new Draw({ source, type: 'Polygon' })

    draw.on('drawstart', () => {
      source.clear()
      setRegionIds([])
    })
    draw.on('drawend', () => setHasArea(true))

    map.addInteraction(draw)

    return () => map.setTarget(undefined)
  }, [])

  // --- Tanımlı alanı yükle ---
  useEffect(() => {
    let cancelled = false

    fetchGeoArea(target.type, target.id)
      .then((area) => {
        if (cancelled) return

        setName(area?.name ?? `${target.label} alanı`)

        if (area?.wkt) {
          sourceRef.current.addFeature(wktToFeature(area.wkt))
          setHasArea(true)
        }
      })
      .catch((err) => {
        if (!cancelled && err.response?.status !== 401) {
          setError('Tanımlı alan okunamadı.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [target])

  const handleClear = useCallback(() => {
    sourceRef.current.clear()
    setRegionIds([])
    setHasArea(false)
  }, [])

  /**
   * Bölge düğmesine basıldığında seçimi açar/kapatır ve haritayı
   * yeniden çizer. Haritadaki şekil her zaman seçimin tamamını
   * gösteriyor - tek tek eklemek yerine baştan kuruyoruz, böylece
   * seçim kaldırıldığında da doğru sonuç çıkıyor.
   */
  const toggleRegion = useCallback((id) => {
    setRegionIds((current) => {
      const next = current.includes(id)
        ? current.filter((r) => r !== id)
        : [...current, id]

      const source = sourceRef.current
      source.clear()

      const wkt = regionsToWkt(next)
      if (wkt) source.addFeature(wktToFeature(wkt))

      setHasArea(Boolean(wkt))
      return next
    })
  }, [])

  async function handleSave() {
    setError('')
    setIsSaving(true)

    const feature = sourceRef.current.getFeatures()[0]

    try {
      await saveGeoArea(target.type, target.id, {
        name: name.trim() || `${target.label} alanı`,
        // Alan silindiyse null gönderiyoruz; sunucu tanımı kaldırıyor.
        wkt: feature ? geometryToWkt(feature.getGeometry()) : null,
      })

      onClose()
    } catch (err) {
      setError(err.response?.data?.message ?? 'Alan kaydedilemedi.')
      setIsSaving(false)
    }
  }

  return (
    <div className="admin-modal__backdrop">
      <div className="geo-dialog">
        <header className="geo-dialog__header">
          <div>
            <h2>Coğrafi yetki — {target.label}</h2>
            <p>
              Haritada bir alan çizin. {target.type === 'role' ? 'Bu roldeki kullanıcılar' : 'Bu kullanıcı'}{' '}
              yalnızca bu alanın içine çizim yapabilir. Alan tanımlanmazsa kısıt uygulanmaz.
            </p>
          </div>
          <button type="button" className="geo-dialog__close" onClick={onClose} aria-label="Kapat">
            ×
          </button>
        </header>

        <div className="admin-field">
          <label htmlFor="geo-name">Alan adı</label>
          <input
            id="geo-name"
            type="text"
            value={name}
            maxLength={100}
            onChange={(e) => setName(e.target.value)}
          />
        </div>

        <div className="geo-dialog__regions">
          <span className="geo-dialog__regions-label">Hazır bölgeler</span>
          <div className="geo-dialog__region-buttons" role="group" aria-label="Coğrafi bölgeler">
            {TURKEY_REGIONS.map((region) => {
              const isOn = regionIds.includes(region.id)

              return (
                <button
                  key={region.id}
                  type="button"
                  className={isOn ? 'geo-region geo-region--on' : 'geo-region'}
                  aria-pressed={isOn}
                  onClick={() => toggleRegion(region.id)}
                >
                  {region.name}
                </button>
              )
            })}
          </div>
        </div>

        <div className="geo-dialog__map" ref={containerRef}>
          {isLoading && <p className="geo-dialog__loading">Yükleniyor…</p>}
        </div>

        <p className="geo-dialog__hint">
          {regionIds.length > 0
            ? `${regionIds.length} bölge seçili. Birden çok bölge seçebilirsiniz; haritaya çizim yaparsanız seçim kalkar.`
            : hasArea
              ? 'Tanımlı alan haritada mavi ile gösteriliyor. Yeni bir poligon çizerseniz eskisinin yerini alır.'
              : 'Yukarıdan hazır bölge seçin ya da köşeleri tıklayıp çift tıklayarak kendi alanınızı çizin.'}
        </p>

        {error && <p className="admin-error">{error}</p>}

        <div className="admin-modal__actions">
          <button
            type="button"
            className="admin-modal__cancel"
            onClick={handleClear}
            disabled={!hasArea || isSaving}
          >
            Alanı kaldır
          </button>
          <button type="button" className="admin-modal__cancel" onClick={onClose} disabled={isSaving}>
            Vazgeç
          </button>
          <button type="button" className="admin-button" onClick={handleSave} disabled={isSaving}>
            {isSaving ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </div>
      </div>
    </div>
  )
}
