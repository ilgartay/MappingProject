import { useEffect, useRef } from 'react'
import Map from 'ol/Map'
import View from 'ol/View'
import TileLayer from 'ol/layer/Tile'
import OSM from 'ol/source/OSM'
import Attribution from 'ol/control/Attribution'
import { fromLonLat, transformExtent } from 'ol/proj'
import 'ol/ol.css'

import { TURKEY_CENTER, TURKEY_EXTENT } from '../map/turkey'
import './LoginMap.css'

/**
 * Login ekranındaki dekoratif Türkiye haritası.
 * Etkileşimsiz: kullanıcı burada gezinmeyecek, sadece arka plan görevi görüyor.
 * Yine de gerçek bir OSM haritası - elle çizilmiş bir silüetin aksine
 * sınırlar doğru ve uygulamanın ne iş yaptığını ilk bakışta anlatıyor.
 */
export default function LoginMap() {
  const containerRef = useRef(null)

  useEffect(() => {
    const map = new Map({
      target: containerRef.current,
      layers: [new TileLayer({ source: new OSM() })],
      // Zoom/pan kapalı; sadece OSM'in zorunlu kaynak gösterimi kalıyor.
      interactions: [],
      controls: [new Attribution({ collapsible: false })],
      view: new View({
        center: fromLonLat(TURKEY_CENTER),
        zoom: 6,
      }),
    })

    // fit() ekran boyutunu bilmek zorunda; getSize() ilk çizimden önce
    // undefined döndüğü için postrender'ı bekliyoruz.
    map.once('postrender', () => {
      const size = map.getSize()
      if (!size) return

      map.getView().fit(transformExtent(TURKEY_EXTENT, 'EPSG:4326', 'EPSG:3857'), {
        size,
        padding: [40, 40, 40, 40],
        maxZoom: 8,
      })
    })

    return () => map.setTarget(undefined)
  }, [])

  return (
    <div className="login-map">
      <div className="login-map__canvas" ref={containerRef} />
      {/* Kurumsal renkte yarı saydam örtü: harita detayı öne çıkmasın,
          üstteki yazı okunur kalsın. */}
      <div className="login-map__tint" aria-hidden="true" />

      <div className="login-map__caption">
        <h2>Harita tabanlı envanter yönetimi</h2>
        <p>Nokta, çizgi ve alan verilerinizi tek ekrandan yönetin.</p>
      </div>
    </div>
  )
}
