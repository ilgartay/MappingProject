import { useEffect, useRef } from 'react'
import Map from 'ol/Map'
import View from 'ol/View'
import TileLayer from 'ol/layer/Tile'
import VectorLayer from 'ol/layer/Vector'
import VectorSource from 'ol/source/Vector'
import OSM from 'ol/source/OSM'
import { fromLonLat, transformExtent } from 'ol/proj'
import 'ol/ol.css'

import { analysisStyle, featureStyle, targetStyle } from './styles'
import { TURKEY_CENTER, TURKEY_EXTENT } from './turkey'

/**
 * OpenLayers haritasını bir kez kurar ve katman kaynaklarını döner.
 * Bileşenin hiçbir state'ine bakmıyor, o yüzden ayrı bir hook olarak duruyor:
 * MapView içindeki asıl iş (çizim, seçim, analiz) bu 50 satırın gürültüsünden
 * kurtulmuş oluyor.
 *
 * @param {React.RefObject<HTMLElement>} containerRef haritanın basılacağı div
 */
export function useMapInstance(containerRef) {
  const mapRef = useRef(null)

  /** Kayıtlı çizimlerin katmanı. */
  const sourceRef = useRef(null)
  const featureLayerRef = useRef(null)

  /** Koordinat aramasının hedef işareti. */
  const targetSourceRef = useRef(null)

  /** Geçici analiz poligonu. */
  const analysisSourceRef = useRef(null)

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
  }, [containerRef])

  return { mapRef, sourceRef, featureLayerRef, targetSourceRef, analysisSourceRef }
}
