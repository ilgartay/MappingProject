import WKT from 'ol/format/WKT'

// Projeksiyon çifti - bu iki satır projenin en kritik yeri:
//   MAP_PROJECTION  : haritanın çalıştığı sistem (OSM döşemeleri metre bazlı)
//   DATA_PROJECTION : veritabanının sakladığı sistem (derece bazlı)
// Aynı nokta 3857'de [3657000, 4859000], 4326'da [32.86, 39.93].
// Çeviriyi atlarsan koordinatlar milyonlarca metre kayar.
export const MAP_PROJECTION = 'EPSG:3857'
export const DATA_PROJECTION = 'EPSG:4326'

const format = new WKT()

/**
 * Haritada çizilen geometriyi veritabanına gidecek WKT metnine çevirir.
 * dataProjection = çıktının sistemi, featureProjection = girdinin sistemi.
 */
export function geometryToWkt(geometry) {
  return format.writeGeometry(geometry, {
    dataProjection: DATA_PROJECTION,
    featureProjection: MAP_PROJECTION,
  })
}

/**
 * Veritabanından gelen WKT metnini haritaya konulabilir feature'a çevirir.
 * Yön ters: girdi 4326, harita için 3857'ye çevriliyor.
 */
export function wktToFeature(wkt) {
  return format.readFeature(wkt, {
    dataProjection: DATA_PROJECTION,
    featureProjection: MAP_PROJECTION,
  })
}
