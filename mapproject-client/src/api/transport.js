import client from './client'

/** Güzergahlar, durakları sıralı halde. Okuma herkese açık. */
export async function fetchRoutes() {
  const { data } = await client.get('/api/Transport/routes')
  return data
}

export async function createRoute(payload) {
  const { data } = await client.post('/api/Transport/routes', payload)
  return data
}

export async function updateRoute(id, payload) {
  const { data } = await client.put(`/api/Transport/routes/${id}`, payload)
  return data
}

export async function deleteRoute(id) {
  await client.delete(`/api/Transport/routes/${id}`)
}

export async function createStop(payload) {
  const { data } = await client.post('/api/Transport/stops', payload)
  return data
}

export async function updateStop(id, payload) {
  const { data } = await client.put(`/api/Transport/stops/${id}`, payload)
  return data
}

export async function deleteStop(id) {
  await client.delete(`/api/Transport/stops/${id}`)
}

/**
 * Sürükle-bırak sonrası yeni sıra.
 *
 * Tek tek "şu durak şu sıraya" yerine tüm listeyi gönderiyoruz: ara
 * durumda iki durağın aynı sıra numarasını taşıması mümkün olmuyor.
 */
export async function reorderStops(routeId, stopIds) {
  const { data } = await client.put(`/api/Transport/routes/${routeId}/order`, { stopIds })
  return data
}

/**
 * "Rota Oluştur": durakların üzerinden geçen sürüş yolunu OSRM'e
 * hesaplatıp güzergaha kaydeder. Güncellenmiş güzergahı döndürür.
 */
export async function buildRoute(routeId) {
  const { data } = await client.post(`/api/Transport/routes/${routeId}/route`)
  return data
}

/** Yeni güzergahlar için hazır renkler; çizim araçlarıyla aynı palet. */
export const ROUTE_COLORS = [
  '#2563eb', '#dc2626', '#059669', '#d97706', '#7c3aed', '#db2777',
]
