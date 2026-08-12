import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

/**
 * Giriş yapılmamışsa login'e yönlendirir.
 * replace: tarayıcı geçmişine yazmaz, yani geri tuşu korumalı sayfaya döndürmez.
 * state.from: giriş sonrası kullanıcıyı gelmek istediği sayfaya geri gönderebilmek için.
 */
export default function ProtectedRoute({ children }) {
  const { isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return children
}
