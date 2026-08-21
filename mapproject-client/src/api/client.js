import axios from 'axios'
import { getToken } from '../auth/authStorage'

/**
 * API'nin kökü. Dışa açık, çünkü OpenLayers'ın WMS kaynağı adresi
 * kendisi kuruyor; axios'un baseURL'i ona yetmiyor.
 */
export const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5215'

const client = axios.create({
  baseURL: API_BASE_URL,
})

// AuthContext açılışta kendi logout fonksiyonunu buraya kaydeder.
// Bunu yapmasak interceptor'dan React state'ine erişemezdik.
let onUnauthorized = null

export function setUnauthorizedHandler(handler) {
  onUnauthorized = handler
}

// İSTEK interceptor'ı: her isteğe token'ı otomatik ekler.
// Tek tek her çağrıda header yazmaktan kurtarıyor.
client.interceptors.request.use((config) => {
  const token = getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// CEVAP interceptor'ı: backend 401 dönerse (token süresi doldu ya da geçersiz)
// oturumu kapat ve login'e gönder.
client.interceptors.response.use(
  (response) => response,
  (error) => {
    const isLoginRequest = error.config?.url?.includes('/api/Auth/login')

    // Login isteğinin kendisi 401 dönebilir (yanlış şifre) - o durumda
    // logout tetiklemek anlamsız, hatayı forma bırakıyoruz.
    if (error.response?.status === 401 && !isLoginRequest) {
      onUnauthorized?.()
    }

    return Promise.reject(error)
  },
)

export default client
