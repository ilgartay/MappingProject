import { useEffect, useRef } from 'react'
import Map from 'ol/Map'
import View from 'ol/View'
import TileLayer from 'ol/layer/Tile'
import VectorLayer from 'ol/layer/Vector'
import VectorSource from 'ol/source/Vector'
import OSM from 'ol/source/OSM'
import { defaults as defaultControls } from 'ol/control/defaults'
import MousePosition from 'ol/control/MousePosition'
import ScaleLine from 'ol/control/ScaleLine'
import { fromLonLat, transformExtent } from 'ol/proj'
import 'ol/ol.css'

import {
  allowedAreaStyle,
  analysisStyle,
  featureStyle,
  poiStyle,
  routeLineStyle,
  stopStyle,
  targetStyle,
} from './styles'
import {
  createFeatureWmsLayer,
  createHeatmapLayer,
  createLocationAnalysisLayer,
  createPoiWmsLayer,
} from './wmsLayers'
import { TURKEY_CENTER, TURKEY_EXTENT } from './turkey'

/**
 * Fare konumunu "39.93312, 32.85997" olarak yazar.
 * Sıra enlem, boylam: koordinat arama kutusu da bu sırayı istiyor,
 * kullanıcı gördüğü değeri doğrudan oraya yapıştırabilsin.
 */
function formatLatLon(coordinate) {
  if (!coordinate) return ''

  const [longitude, latitude] = coordinate
  return `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`
}

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

  /** Kullanıcıya tanımlı çizim alanının sınırı. */
  const areaSourceRef = useRef(null)

  /** Çizimlerin genel gösterimi: GeoServer'ın WMS ile çizdiği resim. */
  const featureWmsRef = useRef(null)

  /** Isı haritası; "Isı Haritası Analizi" açılana kadar gizli. */
  const heatmapRef = useRef(null)

  /**
   * İlgi noktaları. Gösterim WMS'te (kategoriye göre ikon, zoom'a bağlı
   * etiket); vektör katmanı görünmez ama tıklanabilir - çizimlerdeki
   * ayrımın aynısı.
   */
  /** Konum analizinin ısı haritası ve hedef bölge sınırı. */
  const locationAnalysisRef = useRef(null)
  const locationAreaSourceRef = useRef(null)

  /** Ulaşım modülü: hat çizgileri ve duraklar. */
  const routeLineSourceRef = useRef(null)
  const stopSourceRef = useRef(null)
  const stopLayerRef = useRef(null)

  const poiWmsRef = useRef(null)
  const poiSourceRef = useRef(null)
  const poiLayerRef = useRef(null)

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

    const areaSource = new VectorSource()
    areaSourceRef.current = areaSource

    // Vektör katmanı artık görünmüyor: çizimleri WMS gösteriyor. Bu katman
    // yalnızca etkileşim için duruyor - tıklama isabeti, düzenleme ve
    // yeni çizim. Seçili kayıt görünür stille çiziliyor (styles.js).
    const featureLayer = new VectorLayer({ source, style: featureStyle })
    featureLayerRef.current = featureLayer

    const featureWms = createFeatureWmsLayer()
    featureWmsRef.current = featureWms

    const heatmap = createHeatmapLayer()
    heatmapRef.current = heatmap

    const locationAnalysis = createLocationAnalysisLayer()
    locationAnalysisRef.current = locationAnalysis

    // Hedef bölgenin sınırı; analiz alanının nerede bittiği görünsün.
    const locationAreaSource = new VectorSource()
    locationAreaSourceRef.current = locationAreaSource

    const routeLineSource = new VectorSource()
    routeLineSourceRef.current = routeLineSource

    const stopSource = new VectorSource()
    stopSourceRef.current = stopSource

    const stopLayer = new VectorLayer({ source: stopSource, style: stopStyle })
    stopLayerRef.current = stopLayer

    const poiWms = createPoiWmsLayer()
    poiWmsRef.current = poiWms

    // Vektör katmanı görünmüyor; yalnızca tıklama isabeti için duruyor.
    // Seçilen POI görünür hale geliyor (styles.js).
    const poiSource = new VectorSource()
    poiSourceRef.current = poiSource

    const poiLayer = new VectorLayer({ source: poiSource, style: poiStyle })
    poiLayerRef.current = poiLayer

    const map = new Map({
      target: containerRef.current,
      layers: [
        new TileLayer({ source: new OSM() }),
        // Isı haritası altta: üstündeki nokta işaretleri görünmeye devam
        // etsin, kullanıcı hangi noktaların yoğunluğu oluşturduğunu görsün.
        heatmap,
        // Genel gösterim - sunucuda çizilmiş resimler.
        featureWms,
        poiWms,
        // Hat çizgisi durakların altında kalsın.
        new VectorLayer({ source: routeLineSource, style: routeLineStyle }),
        stopLayer,
        // Konum analizi ısı haritası POI'lerin ÜSTÜNDE.
        //
        // Altta dururken POI işaretleri ve beyaz konturlu etiketleri tam
        // ısının yoğunlaştığı yere yığılıp sonucu kapatıyordu - ısı
        // zaten POI'lerin olduğu yerde çıkıyor. Üstte ve yarı saydam
        // olunca hem desen okunuyor hem işaretler altından görünüyor.
        locationAnalysis,
        new VectorLayer({ source: locationAreaSource, style: analysisStyle }),
        // Etkileşim katmanı: görünmez, ama tıklanabilir ve düzenlenebilir.
        featureLayer,
        poiLayer,
        new VectorLayer({ source: areaSource, style: allowedAreaStyle }),
        new VectorLayer({ source: analysisSource, style: analysisStyle }),
        new VectorLayer({ source: targetSource, style: targetStyle }),
      ],
      view: new View({
        center: fromLonLat(TURKEY_CENTER),
        zoom: 6,
        minZoom: 3,
      }),
      // Varsayılan kontrollerin (zoom, kaynak gösterimi) üstüne ölçek
      // çubuğu ve fare konumu ekleniyor: harita hangi ölçekte, imleç
      // nereye denk geliyor - ikisi de artık okunabiliyor.
      controls: defaultControls().extend([
        new ScaleLine(),
        new MousePosition({
          // Harita 3857 ama kullanıcıya derece gösteriyoruz: veritabanında
          // da, arama kutusunda da 4326 kullanılıyor.
          projection: 'EPSG:4326',
          coordinateFormat: formatLatLon,
          placeholder: '-',
        }),
      ]),
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

  return {
    mapRef,
    sourceRef,
    featureLayerRef,
    targetSourceRef,
    analysisSourceRef,
    areaSourceRef,
    featureWmsRef,
    heatmapRef,
    locationAnalysisRef,
    locationAreaSourceRef,
    routeLineSourceRef,
    stopSourceRef,
    stopLayerRef,
    poiWmsRef,
    poiSourceRef,
    poiLayerRef,
  }
}
