import client from './client'

/**
 * Poligonla kesişen envanterleri sayar. Poligon veritabanına yazılmaz.
 * @param {string} wkt EPSG:4326 poligon
 * @param {number} [excludePolygonId] kayıtlı poligonun kendisini saymamak için
 */
export async function analyzeIntersection(wkt, excludePolygonId) {
  const { data } = await client.post('/api/Analysis/intersect', {
    wkt,
    excludePolygonId: excludePolygonId ?? null,
  })
  return data
}
