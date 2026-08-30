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

/**
 * Oturum sayacı. Asıl çıkış işlemini AuthContext yapıyor; burası
 * yalnızca kalan süreyi gösteriyor.
 *
 * Kendi bileşeni olmasının sebebi başlıkta düzen değil, başarım:
 * saniyede bir değişen bir durum MapPage'de dursaydı, MapPage'in
 * altındaki her şey - haritanın tamamı - saniyede bir yeniden
 * render edilirdi. Durum burada kalınca yenilenen tek şey bu span.
 */
function SessionTimer({ expiresAt }) {
  const [remaining, setRemaining] = useState(() =>
    expiresAt ? getRemainingMs(expiresAt) : 0,
  )

  useEffect(() => {
    if (!expiresAt) return

    // İlk değeri useState hesaplıyor; burada tekrar yazmak gereksiz
    // bir render turu açardı.
    const interval = setInterval(() => {
      setRemaining(getRemainingMs(expiresAt))
    }, 1000)

    return () => clearInterval(interval)
  }, [expiresAt])

  return (
    <span className="map-header__timer" title="Oturum süresi">
      {formatRemaining(remaining)}
    </span>
  )
}

export default function MapPage() {
  const { username, expiresAt, logout, hasPermission } = useAuth()

  // Yönetim bağlantısı sadece panelde görecek bir şeyi olana görünsün.
  // Ulaşım yetkileri de sayılıyor: Ulaşım Operatörü'nün admin panelinde
  // yalnızca Ulaşım Yönetimi ekranı açılıyor, diğer menüler yetkisi
  // olmadığı için hiç çizilmiyor.
  const canManage = ['user.manage', 'role.manage', 'route.manage', 'stop.manage']
    .some((code) => hasPermission(code))

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
          <SessionTimer expiresAt={expiresAt} />
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
