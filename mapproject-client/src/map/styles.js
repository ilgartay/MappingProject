import { Circle, Fill, Stroke, Style, Text } from 'ol/style'

// Her geometri tipi farklı renkte: kullanıcı hangi katmana ne çizdiğini
// tek bakışta ayırt edebilsin.
const POINT_COLOR = '#009bff'
const LINE_COLOR = '#db2777'
const POLYGON_COLOR = '#059669'

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

const pointStyle = (name) =>
  new Style({
    image: new Circle({
      radius: 6,
      fill: new Fill({ color: POINT_COLOR }),
      stroke: new Stroke({ color: '#ffffff', width: 2 }),
    }),
    text: label(name),
  })

const lineStyle = (name) =>
  new Style({
    stroke: new Stroke({ color: LINE_COLOR, width: 3 }),
    text: label(name),
  })

const polygonStyle = (name) =>
  new Style({
    stroke: new Stroke({ color: POLYGON_COLOR, width: 2 }),
    fill: new Fill({ color: 'rgba(5, 150, 105, 0.18)' }),
    text: label(name),
  })

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

/** Katmandaki her feature için geometri tipine göre stil seçer. */
export function featureStyle(feature) {
  const name = feature.get('name')

  switch (feature.getGeometry()?.getType()) {
    case 'Point':
      return pointStyle(name)
    case 'LineString':
      return lineStyle(name)
    case 'Polygon':
      return polygonStyle(name)
    default:
      return undefined
  }
}
