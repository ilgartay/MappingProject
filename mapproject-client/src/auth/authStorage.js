// Token'ı tek bir yerden yönetiyoruz. Böylece localStorage anahtarları
// kodun her yerine dağılmıyor; ileride sessionStorage'a geçmek istersek
// sadece bu dosya değişir.

const TOKEN_KEY = 'mapproject.token'
const EXPIRES_KEY = 'mapproject.expiresAt'
const USERNAME_KEY = 'mapproject.username'

export function saveSession({ token, expiresAt, username }) {
  localStorage.setItem(TOKEN_KEY, token)
  localStorage.setItem(EXPIRES_KEY, expiresAt)
  localStorage.setItem(USERNAME_KEY, username)
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(EXPIRES_KEY)
  localStorage.removeItem(USERNAME_KEY)
}

export function getToken() {
  return localStorage.getItem(TOKEN_KEY)
}

/**
 * Kaydedilmiş oturumu okur.
 * Süresi geçmişse temizleyip null döner - böylece sekmeyi kapatıp
 * 20 dakika sonra açan kullanıcı "giriş yapmış" görünmüyor.
 */
export function getSession() {
  const token = localStorage.getItem(TOKEN_KEY)
  const expiresAt = localStorage.getItem(EXPIRES_KEY)
  const username = localStorage.getItem(USERNAME_KEY)

  if (!token || !expiresAt) return null

  if (getRemainingMs(expiresAt) <= 0) {
    clearSession()
    return null
  }

  return { token, expiresAt, username }
}

/** Token'ın dolmasına kaç milisaniye kaldı. Backend UTC gönderiyor. */
export function getRemainingMs(expiresAt) {
  // Backend'in "2026-08-12T12:49:29.83395Z" formatı UTC. Sonunda Z yoksa
  // tarayıcı yerel saat sanar ve sayaç 3 saat şaşar - o yüzden kontrol ediyoruz.
  const iso = expiresAt.endsWith('Z') ? expiresAt : `${expiresAt}Z`
  return new Date(iso).getTime() - Date.now()
}
