import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr'
import client, { API_BASE_URL } from './client'
import { getToken } from '../auth/authStorage'

// --- REST ---

/** "Simülasyonu Başlat". Yalnızca route.manage yetkisi olanlar çağırabiliyor. */
export async function startSimulation(routeId) {
  const { data } = await client.post(`/api/Transport/routes/${routeId}/simulation`)
  return data
}

export async function stopSimulation(routeId) {
  await client.delete(`/api/Transport/routes/${routeId}/simulation`)
}

/**
 * O anda yürüyen simülasyonlar.
 *
 * Sayfa açılışında bir kez okunuyor: yalnızca SignalR dinleseydik,
 * simülasyon başladıktan sonra giren kullanıcı hangi hatlarda araç
 * olduğunu bir sonraki yayına kadar bilemezdi.
 */
export async function fetchSimulations() {
  const { data } = await client.get('/api/Transport/simulations')
  return data
}

// --- SignalR ---

/**
 * Canlı yayın bağlantısını kurar.
 *
 * accessTokenFactory her yeniden bağlanmada çağrılıyor; token'ı bir kez
 * okuyup saklasaydık, bağlantı koptuktan sonra süresi dolmuş token'la
 * dönmeye çalışırdı.
 *
 * Taşıma WebSocket'e sabitlenmiş. Varsayılan sıralama önce WebSocket'i
 * deniyor zaten, ama başarısız olursa uzun yoklamaya (long polling)
 * düşüyor ve sorun sessizce gizleniyor - burada bağlantı sorununu
 * görmek istiyoruz.
 */
export function createSimulationConnection() {
  return new HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/hubs/simulation`, {
      accessTokenFactory: () => getToken() ?? '',
      transport: HttpTransportType.WebSockets,
      skipNegotiation: true,
    })
    // Bağlantı koparsa kendiliğinden dönsün; araç takibi yarıda kalmasın.
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
}
