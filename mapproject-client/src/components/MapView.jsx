import { useEffect, useRef } from 'react'
import Map from 'ol/Map'
import View from 'ol/View'
import TileLayer from 'ol/layer/Tile'
import OSM from 'ol/source/OSM'
import { fromLonLat, transformExtent } from 'ol/proj'
import 'ol/ol.css'
import './MapView.css'

// Türkiye'nin yaklaşık sınır kutusu: [batı, güney, doğu, kuzey] (derece cinsinden)
const TURKEY_EXTENT = [25.5, 35.7, 45.0, 42.3]

// Kaba merkez: Ankara'nın biraz batısı. Harita ilk karede burayı gösterir,
// sonraki karede fit() ekrana göre ince ayar yapar.
const TURKEY_CENTER = [35.2, 39.0]

export default function MapView() {
  const containerRef = useRef(null)

  useEffect(() => {
    const map = new Map({
      target: containerRef.current,
      layers: [
        // OSM = OpenStreetMap altlık katmanı. Ücretsiz ve anahtar istemiyor.
        new TileLayer({ source: new OSM() }),
      ],
      view: new View({
        // OSM döşemeleri EPSG:3857 (metre) kullanıyor, bizim koordinatlar ise
        // EPSG:4326 (derece). fromLonLat ikisi arasında çeviri yapıyor.
        center: fromLonLat(TURKEY_CENTER),
        zoom: 6,
        minZoom: 3,
      }),
    })

    // Sabit zoom yerine extent'e oturtuyoruz: aynı zoom değeri geniş ekranda
    // Türkiye'yi tam gösterirken telefonda sadece İç Anadolu'yu gösterirdi.
    // fit() ekran boyutunu bildiği için her cihazda ülkenin tamamı görünür.
    map.once('postrender', () => {
      const size = map.getSize()
      if (!size) return

      map.getView().fit(transformExtent(TURKEY_EXTENT, 'EPSG:4326', 'EPSG:3857'), {
        size,
        padding: [24, 24, 24, 24],
        maxZoom: 9,
      })
    })

    // React 18+ StrictMode geliştirme modunda effect'i iki kez çalıştırır.
    // Temizlemezsek iki harita üst üste binerdi.
    return () => map.setTarget(undefined)
  }, [])

  return <div className="map-view" ref={containerRef} />
}
