import { useCallback, useEffect, useMemo, useState } from 'react'
import client, { setUnauthorizedHandler } from '../api/client'
import { clearSession, getRemainingMs, getSession, saveSession } from './authStorage'
import { AuthContext } from './authContext'

export function AuthProvider({ children }) {
  // Açılışta localStorage'a bakıyoruz: sayfa yenilenince oturum kaybolmasın.
  // getSession süresi geçmiş oturumu zaten temizleyip null döndürüyor.
  const [session, setSession] = useState(() => getSession())

  const logout = useCallback(() => {
    clearSession()
    setSession(null)
  }, [])

  const login = useCallback(async (username, password) => {
    const { data } = await client.post('/api/Auth/login', { username, password })
    saveSession(data)
    setSession(data)
  }, [])

  // 401 gelirse interceptor bu fonksiyonu çağıracak.
  useEffect(() => {
    setUnauthorizedHandler(logout)
    return () => setUnauthorizedHandler(null)
  }, [logout])

  // Token'ın süresi dolduğu anda kullanıcıyı bekletmeden çıkar.
  // Sadece 401'e güvenseydik, kullanıcı bir istek atana kadar oturumda kalmış görünürdü.
  useEffect(() => {
    if (!session) return

    // Negatifse setTimeout 0 gibi davranır, yani hemen çıkış yapılır.
    const remaining = Math.max(0, getRemainingMs(session.expiresAt))
    const timer = setTimeout(logout, remaining)

    return () => clearTimeout(timer)
  }, [session, logout])

  const value = useMemo(
    () => ({
      isAuthenticated: session !== null,
      username: session?.username ?? null,
      expiresAt: session?.expiresAt ?? null,
      login,
      logout,
    }),
    [session, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
