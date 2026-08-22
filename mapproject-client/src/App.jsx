import { Navigate, Route, Routes } from 'react-router-dom'
import ProtectedRoute from './components/ProtectedRoute'
import LoginPage from './pages/LoginPage'
import MapPage from './pages/MapPage'
import AdminLayout from './pages/admin/AdminLayout'
import UsersPage from './pages/admin/UsersPage'
import PoiPage from './pages/admin/PoiPage'
import RolesPage from './pages/admin/RolesPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route
        path="/map"
        element={
          <ProtectedRoute>
            <MapPage />
          </ProtectedRoute>
        }
      />

      {/* Admin paneli: harita uygulamasıyla aynı oturumu paylaşıyor,
          ayrı bir kabuk (dikey menü + içerik) altında çalışıyor. */}
      <Route
        path="/admin"
        element={
          <ProtectedRoute>
            <AdminLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/admin/users" replace />} />
        <Route path="users" element={<UsersPage />} />
        <Route path="roles" element={<RolesPage />} />
        <Route path="poi" element={<PoiPage />} />
      </Route>

      {/* Bilinmeyen her adres haritaya gider; giriş yoksa ProtectedRoute login'e atar. */}
      <Route path="*" element={<Navigate to="/map" replace />} />
    </Routes>
  )
}
