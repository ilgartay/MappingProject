import client from './client'

/** Üç tablodaki tüm geometrileri WKT olarak getirir. */
export async function fetchFeatures() {
  const { data } = await client.get('/api/Feature')
  return data
}

/**
 * @param {'point'|'line'|'polygon'} type
 * @param {{name: string, wkt: string}} payload
 */
export async function createFeature(type, payload) {
  const { data } = await client.post(`/api/Feature/${type}`, payload)
  return data
}

/**
 * @param {'point'|'line'|'polygon'} type
 * @param {number} id
 */
export async function deleteFeature(type, id) {
  await client.delete(`/api/Feature/${type}/${id}`)
}

/** OpenLayers geometri tipi -> API uç noktası. */
export const ENDPOINT_BY_GEOMETRY = {
  Point: 'point',
  LineString: 'line',
  Polygon: 'polygon',
}
