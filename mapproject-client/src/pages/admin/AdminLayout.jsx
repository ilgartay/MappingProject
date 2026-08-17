import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../../auth/useAuth'
import './admin.css'

const MENU = [
  {
    to: '/admin/users',
    label: 'Kullanıcı Listesi',
    permission: 'user.manage',
    icon: (
      <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M16 20v-1.5A3.5 3.5 0 0 0 12.5 15h-5A3.5 3.5 0 0 0 4 18.5V20" />
        <circle cx="10" cy="8" r="3.5" />
        <path d="M19 20v-1.5a3.5 3.5 0 0 0-2.5-3.35M15.5 5.2a3.5 3.5 0 0 1 0 5.6" />
      </svg>
    ),
  },
  {
    to: '/admin/roles',
    label: 'Rol Listesi',
    permission: 'role.manage',
    icon: (
      <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M12 3 4 6.5v5c0 4.4 3.2 8.4 8 9.5 4.8-1.1 8-5.1 8-9.5v-5L12 3z" />
        <path d="m9 12 2 2 4-4" />
      </svg>
    ),
  },
]

/**
 * Admin panelinin kabuğu: solda dikey menü, sağda seçilen ekran.
 * Menü öğeleri kullanıcının yetkisine göre görünüyor.
 */
export default function AdminLayout() {
  const { username, hasPermission, logout } = useAuth()
  const visibleMenu = MENU.filter((item) => hasPermission(item.permission))

  return (
    <div className="admin">
      <aside className="admin__sidebar">
        <div className="admin__brand">
          <img src="/basarsoft.svg" alt="Başarsoft" />
        </div>

        <p className="admin__section">Yönetim</p>

        <nav className="admin__nav">
          {visibleMenu.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                isActive ? 'admin__link admin__link--active' : 'admin__link'
              }
            >
              {item.icon}
              <span>{item.label}</span>
            </NavLink>
          ))}

          {visibleMenu.length === 0 && (
            <p className="admin__empty-menu">Yönetim yetkiniz yok.</p>
          )}
        </nav>

        <div className="admin__sidebar-footer">
          <Link to="/map" className="admin__back">
            ← Haritaya dön
          </Link>
          <div className="admin__user">
            <span>{username}</span>
            <button type="button" onClick={logout}>
              Çıkış
            </button>
          </div>
        </div>
      </aside>

      <main className="admin__content">
        <Outlet />
      </main>
    </div>
  )
}
