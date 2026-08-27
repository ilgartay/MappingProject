import { Circle, Fill, RegularShape, Stroke, Style, Text } from 'ol/style'

/** Renk bilgisi gelmezse kullanılacak yedek. */
const FALLBACK_COLOR = '#009bff'

/** Tamamen saydam - çizilir ama görünmez. */
const TRANSPARENT = 'rgba(0, 0, 0, 0)'

/** "#009bff" -> "rgba(0, 155, 255, 0.18)" - poligon dolgusu için. */
function withAlpha(hex, alpha) {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

/**
 * Görünmeyen ama tıklanabilen stil.
 *
 * Çizimlerin genel gösterimi artık GeoServer'ın ürettiği WMS resminde;
 * vektör katmanı sadece etkileşim için duruyor. Katmanı büsbütün gizlemek
 * (visible: false) tıklamayı da öldürürdü - OpenLayers isabet testini
 * stilin ürettiği geometriye göre yapıyor. Bu yüzden şekli çiziyoruz ama
 * tamamen saydam renkle.
 *
 * Ölçüler bilerek geniş: çizgiye tam üstünden basmak zor, 12 piksellik
 * saydam kalınlık tıklamayı kolaylaştırıyor.
 */
const INTERACTION_ONLY = new Style({
  image: new Circle({ radius: 9, fill: new Fill({ color: TRANSPARENT }) }),
  stroke: new Stroke({ color: TRANSPARENT, width: 12 }),
  fill: new Fill({ color: TRANSPARENT }),
})

/**
 * Katmandaki her feature için stil üretir.
 *
 * Kayıtlı çizimler görünmez (WMS onları zaten gösteriyor); yalnızca
 * kullanıcının o an dokunduğu kayıt görünür hale geliyor. 'interactive'
 * bayrağını MapView koyuyor: seçilen kayıt ve henüz kaydedilmemiş çizim.
 *
 * Etiket yok: seçili kaydın adını WMS zaten yazıyor, burada da yazarsak
 * aynı metin üst üste iki kere çıkıyor. Kaydedilmemiş çizimin ise
 * henüz adı yok.
 */
export function featureStyle(feature) {
  if (!feature.get('interactive')) {
    return INTERACTION_ONLY
  }

  const color = feature.get('color') ?? FALLBACK_COLOR

  switch (feature.getGeometry()?.getType()) {
    case 'Point':
      return new Style({
        image: new Circle({
          radius: 6,
          fill: new Fill({ color }),
          stroke: new Stroke({ color: '#ffffff', width: 2 }),
        }),
      })
    case 'LineString':
      return new Style({
        stroke: new Stroke({ color, width: 3 }),
      })
    case 'Polygon':
      return new Style({
        stroke: new Stroke({ color, width: 2 }),
        fill: new Fill({ color: withAlpha(color, 0.18) }),
      })
    default:
      return undefined
  }
}

/**
 * POI işareti.
 *
 * Kayıtlı POI'leri WMS gösteriyor (kategorisine göre ikon, yakınlaşınca
 * isim), bu yüzden vektör katmanı normalde görünmez - yalnızca tıklama
 * isabeti için duruyor. Kullanıcının seçtiği ya da henüz kaydetmediği
 * POI 'interactive' işaretiyle görünür hale geliyor.
 */
export function poiStyle(feature) {
  if (!feature.get('interactive')) {
    return INTERACTION_ONLY
  }

  return new Style({
    image: new RegularShape({
      points: 4,
      radius: 9,
      angle: Math.PI / 4,
      fill: new Fill({ color: '#e11d48' }),
      stroke: new Stroke({ color: '#ffffff', width: 2 }),
    }),
  })
}

/**
 * Durak işareti: bağlı olduğu güzergahın renginde, içinde sıra numarası.
 *
 * POI'lerden farklı olarak duraklar WMS ile değil vektör olarak
 * çiziliyor. Gerekçe ölçek: durak sayısı onlarla ifade ediliyor ve modül
 * düzenleme ağırlıklı - sıra değişince ya da güzergahın rengi
 * güncellenince sonucun anında görünmesi gerekiyor. Sunucudan resim
 * beklemek burada yavaşlatırdı.
 */
export function stopStyle(feature) {
  const color = feature.get('routeColor') ?? FALLBACK_COLOR

  return new Style({
    image: new Circle({
      radius: 10,
      fill: new Fill({ color }),
      stroke: new Stroke({ color: '#ffffff', width: 2 }),
    }),
    text: new Text({
      // Sıra numarası işaretin içinde: hattın hangi yönde ilerlediği
      // haritaya bakınca anlaşılsın.
      text: String(feature.get('order') ?? ''),
      font: '700 11px system-ui, sans-serif',
      fill: new Fill({ color: '#ffffff' }),
    }),
  })
}

/** Durakları sırayla birleştiren hat çizgisi. */
export function routeLineStyle(feature) {
  return new Style({
    stroke: new Stroke({
      color: feature.get('routeColor') ?? FALLBACK_COLOR,
      width: 4,
      lineCap: 'round',
      lineJoin: 'round',
    }),
  })
}

/**
 * Koordinat aramasında gidilen noktayı işaretler.
 * Çizimlerden ayrışsın diye içi boş kırmızı halka.
 */
export const targetStyle = new Style({
  image: new Circle({
    radius: 10,
    stroke: new Stroke({ color: '#dc2626', width: 3 }),
  }),
})

/**
 * Kullanıcıya tanımlı çizim alanının sınırı.
 * Dolgusu yok: altındaki harita ve çizimler görünmeye devam etsin,
 * sadece nereye çizebileceği belli olsun.
 */
export const allowedAreaStyle = new Style({
  stroke: new Stroke({ color: '#7c3aed', width: 2, lineDash: [10, 6] }),
})

/**
 * Geçici analiz poligonu. Kesikli çizgi, "bu kaydedilmeyecek" mesajını
 * görsel olarak veriyor - kayıtlı poligonlar düz çizgili.
 */
export const analysisStyle = new Style({
  stroke: new Stroke({ color: '#f59e0b', width: 2.5, lineDash: [8, 6] }),
  fill: new Fill({ color: 'rgba(245, 158, 11, 0.12)' }),
})
