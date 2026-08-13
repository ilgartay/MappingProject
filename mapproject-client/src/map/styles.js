import { Circle, Fill, Stroke, Style, Text } from 'ol/style'

/** Renk bilgisi gelmezse kullanılacak yedek. */
const FALLBACK_COLOR = '#009bff'

/** "#009bff" -> "rgba(0, 155, 255, 0.18)" - poligon dolgusu için. */
function withAlpha(hex, alpha) {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

function label(text) {
  return new Text({
    text: text ?? '',
    font: '600 12px system-ui, sans-serif',
    offsetY: -14,
    fill: new Fill({ color: '#0f172a' }),
    // Beyaz kontur, etiketin harita üzerinde okunabilir kalmasını sağlıyor.
    stroke: new Stroke({ color: '#ffffff', width: 3 }),
  })
}

/**
 * Katmandaki her feature için stil üretir.
 * Şekli geometri tipi, rengi ise kullanıcının kaydettiği değer belirliyor.
 */
export function featureStyle(feature) {
  const name = feature.get('name')
  const color = feature.get('color') ?? FALLBACK_COLOR

  switch (feature.getGeometry()?.getType()) {
    case 'Point':
      return new Style({
        image: new Circle({
          radius: 6,
          fill: new Fill({ color }),
          stroke: new Stroke({ color: '#ffffff', width: 2 }),
        }),
        text: label(name),
      })
    case 'LineString':
      return new Style({
        stroke: new Stroke({ color, width: 3 }),
        text: label(name),
      })
    case 'Polygon':
      return new Style({
        stroke: new Stroke({ color, width: 2 }),
        fill: new Fill({ color: withAlpha(color, 0.18) }),
        text: label(name),
      })
    default:
      return undefined
  }
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
 * Geçici analiz poligonu. Kesikli çizgi, "bu kaydedilmeyecek" mesajını
 * görsel olarak veriyor - kayıtlı poligonlar düz çizgili.
 */
export const analysisStyle = new Style({
  stroke: new Stroke({ color: '#f59e0b', width: 2.5, lineDash: [8, 6] }),
  fill: new Fill({ color: 'rgba(245, 158, 11, 0.12)' }),
})
