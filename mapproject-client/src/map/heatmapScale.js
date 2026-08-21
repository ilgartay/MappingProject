/**
 * Isı haritasının renk basamakları.
 *
 * Bu dizi geoserver/styles/mapproject_heatmap.sld içindeki ColorMapEntry
 * değerlerinin birebir aynısı. İkisi ayrı yerlerde durduğu için biri
 * değişirse diğeri de değişmeli - lejant yanlış renk gösterirse
 * kullanıcı haritayı yanlış okur.
 */
export const HEATMAP_STOPS = [
  { value: 0.0, color: '#2c7bb6' },
  { value: 0.2, color: '#2c7bb6' },
  { value: 0.4, color: '#abd9e9' },
  { value: 0.6, color: '#ffffbf' },
  { value: 0.8, color: '#fdae61' },
  { value: 1.0, color: '#d7191c' },
]

/** SLD'deki "ramp" tipini CSS gradyanı olarak yeniden üretir. */
export const HEATMAP_GRADIENT = `linear-gradient(to right, ${HEATMAP_STOPS.map(
  (stop) => `${stop.color} ${stop.value * 100}%`,
).join(', ')})`
