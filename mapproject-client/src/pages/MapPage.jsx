import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { getRemainingMs } from '../auth/authStorage'
import MapView from '../components/MapView'
import './MapPage.css'

/** 125000 ms -> "02:05" */
function formatRemaining(ms) {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000))
  const minutes = String(Math.floor(totalSeconds / 60)).padStart(2, '0')
  const seconds = String(totalSeconds % 60).padStart(2, '0')
  return `${minutes}:${seconds}`
}

export default function MapPage() {
  const { username, expiresAt, logout, hasPermission } = useAuth()

  // Yönetim bağlantısı sadece yetkisi olana görünsün.
  const canManage = hasPermission('user.manage') || hasPermission('role.manage')
  const [remaining, setRemaining] = useState(() =>
    expiresAt ? getRemainingMs(expiresAt) : 0,
  )

  // Oturum sayacı. Asıl çıkış işlemini AuthContext yapıyor;
  // burası sadece kalan süreyi gösteriyor.
  useEffect(() => {
    if (!expiresAt) return

    const interval = setInterval(() => {
      setRemaining(getRemainingMs(expiresAt))
    }, 1000)

    return () => clearInterval(interval)
  }, [expiresAt])

  return (
    <div className="map-page">
      <header className="map-header">
        {/* Logo public/ altında duruyor: resmi asset geldiğinde tek dosya
            değiştirmek yeterli, bu bileşene dokunmaya gerek kalmıyor. */}
        <img className="map-header__logo" src="/basarsoft.svg" alt="Başarsoft" />

        <div className="map-header__actions">
          {canManage && (
            <Link to="/admin" className="map-header__admin">
              Yönetim
            </Link>
          )}
          <span className="map-header__timer" title="Oturum süresi">
            {formatRemaining(remaining)}
          </span>
          <span className="map-header__user">{username}</span>
          <button type="button" className="map-header__logout" onClick={logout}>
            Çıkış
          </button>
        </div>
      </header>

      <main className="map-body">
        <MapView />
      </main>
    </div>
  )
}
