import { useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import './LoginPage.css'

export default function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Giriş yapmış kullanıcı login sayfasını görmesin.
  if (isAuthenticated) {
    return <Navigate to="/map" replace />
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      await login(username.trim(), password)
      // Korumalı bir sayfadan yönlendirildiyse oraya geri dön, yoksa haritaya.
      navigate(location.state?.from ?? '/map', { replace: true })
    } catch (err) {
      if (err.response?.status === 401) {
        setError(err.response.data?.message ?? 'Kullanıcı adı veya şifre hatalı.')
      } else if (err.response) {
        setError('Sunucu hatası oluştu. Lütfen tekrar deneyin.')
      } else {
        setError('Sunucuya ulaşılamıyor. API çalışıyor mu?')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="login-page">
      <section className="login-card">
        <header className="login-card__header">
          {/* Logo public/ altında: resmi asset gelince tek dosya değişecek. */}
          <img className="login-card__logo" src="/basarsoft.svg" alt="Başarsoft" />
          <h1>Harita Uygulaması</h1>
          <p>Devam etmek için giriş yapın</p>
        </header>

        <form className="login-form" onSubmit={handleSubmit} noValidate>
          <div className="login-field">
            <label htmlFor="username">Kullanıcı adı</label>
            <input
              id="username"
              name="username"
              type="text"
              autoComplete="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="admin"
              required
              autoFocus
            />
          </div>

          <div className="login-field">
            <label htmlFor="password">Şifre</label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
            />
          </div>

          {/* role="alert" ekran okuyucunun hatayı sesli okumasını sağlar. */}
          {error && (
            <p className="login-error" role="alert">
              {error}
            </p>
          )}

          <button type="submit" className="login-button" disabled={isSubmitting}>
            {isSubmitting ? 'Giriş yapılıyor…' : 'Giriş yap'}
          </button>
        </form>
      </section>
    </main>
  )
}
