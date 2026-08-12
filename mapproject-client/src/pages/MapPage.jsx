import { useEffect, useState } from 'react'
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
  const { username, expiresAt, logout } = useAuth()
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
        <div className="map-header__brand">
          <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M12 21s-7-5.686-7-11a7 7 0 1 1 14 0c0 5.314-7 11-7 11z" />
            <circle cx="12" cy="10" r="2.5" />
          </svg>
          <span>MapProject</span>
        </div>

        <div className="map-header__actions">
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
