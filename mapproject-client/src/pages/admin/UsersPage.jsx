import { useCallback, useEffect, useState } from 'react'
import {
  createUser,
  deleteUser,
  fetchRoles,
  fetchUserAccess,
  fetchUsers,
  saveUserAccess,
  updateUser,
} from '../../api/admin'
import { useAuth } from '../../auth/useAuth'
import AdminSearch from './AdminSearch'
import { matchesQuery } from './adminFilter'
import GeoAreaDialog from './GeoAreaDialog'

const EMPTY_FORM = { username: '', password: '', isActive: true, roleIds: [] }

export default function UsersPage() {
  const { hasPermission } = useAuth()
  const canManageGeo = hasPermission('geo.manage')

  const [geoTarget, setGeoTarget] = useState(null) // {type, id, label}
  const [query, setQuery] = useState('')
  const [users, setUsers] = useState([])
  const [roles, setRoles] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  const [form, setForm] = useState(null) // {id: null|number, ...}
  const [access, setAccess] = useState(null) // yetki sekmesi
  const [isSaving, setIsSaving] = useState(false)
  const [formError, setFormError] = useState('')

  // Veri çekme ile state yazma ayrı: effect içinde doğrudan setState
  // zinciri kurmak cascading render'a yol açıyor (react-hooks kuralı).
  const fetchAll = useCallback(() => Promise.all([fetchUsers(), fetchRoles()]), [])

  const applyData = useCallback(([userList, roleList]) => {
    setUsers(userList)
    setRoles(roleList)
    setError('')
  }, [])

  const load = useCallback(
    () => fetchAll().then(applyData),
    [fetchAll, applyData],
  )

  useEffect(() => {
    let cancelled = false

    fetchAll()
      .then((data) => {
        if (!cancelled) applyData(data)
      })
      .catch((err) => {
        if (!cancelled && err.response?.status !== 401) {
          setError('Kullanıcılar yüklenemedi.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [fetchAll, applyData])

  function openCreate() {
    setFormError('')
    setForm({ id: null, ...EMPTY_FORM })
  }

  function openEdit(user) {
    setFormError('')
    setForm({
      id: user.id,
      username: user.username,
      password: '',
      isActive: user.isActive,
      roleIds: [...user.roleIds],
    })
  }

  async function openAccess(user) {
    setFormError('')

    try {
      const data = await fetchUserAccess(user.id)
      setAccess(data)
    } catch (err) {
      if (err.response?.status !== 401) setError('Yetki bilgisi alınamadı.')
    }
  }

  function toggleFormRole(id) {
    setForm((current) => ({
      ...current,
      roleIds: current.roleIds.includes(id)
        ? current.roleIds.filter((r) => r !== id)
        : [...current.roleIds, id],
    }))
  }

  function toggleDirectPermission(id) {
    setAccess((current) => ({
      ...current,
      permissions: current.permissions.map((p) =>
        p.id === id ? { ...p, isDirect: !p.isDirect } : p,
      ),
    }))
  }

  async function handleSubmit(event) {
    event.preventDefault()

    if (!form.username.trim()) {
      setFormError('Kullanıcı adı zorunludur.')
      return
    }

    if (form.id === null && form.password.length < 6) {
      setFormError('Yeni kullanıcı için en az 6 karakterlik şifre girin.')
      return
    }

    setFormError('')
    setIsSaving(true)

    const payload = {
      username: form.username.trim(),
      // Güncellemede boş şifre gönderilirse sunucu mevcut şifreyi koruyor.
      password: form.password ? form.password : null,
      isActive: form.isActive,
      roleIds: form.roleIds,
    }

    try {
      if (form.id === null) {
        await createUser(payload)
      } else {
        await updateUser(form.id, payload)
      }

      setForm(null)
      await load()
    } catch (err) {
      setFormError(err.response?.data?.message ?? 'Kaydedilemedi.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleSaveAccess() {
    setFormError('')
    setIsSaving(true)

    try {
      await saveUserAccess(access.userId, {
        roleIds: access.roleIds,
        // Sadece doğrudan işaretlenenler; rolden gelenler zaten kilitli.
        permissionIds: access.permissions.filter((p) => p.isDirect).map((p) => p.id),
      })

      setAccess(null)
      await load()
    } catch (err) {
      setFormError(err.response?.data?.message ?? 'Yetkiler kaydedilemedi.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDelete(user) {
    if (!window.confirm(`"${user.username}" kullanıcısı silinsin mi?`)) return

    try {
      await deleteUser(user.id)
      await load()
    } catch (err) {
      setError(err.response?.data?.message ?? 'Kullanıcı silinemedi.')
    }
  }

  // Kullanıcı adının yanında rol adları da taranıyor: "Operatör" yazan
  // o rolü taşıyan herkesi görsün.
  const visibleUsers = users.filter((user) =>
    matchesQuery(query, user.username, ...user.roleNames),
  )

  return (
    <>
      <header className="admin-page__header">
        <div>
          <h1>Kullanıcı Listesi</h1>
          <p>Kullanıcı ekleyin, bilgilerini güncelleyin, rol ve yetkilerini yönetin.</p>
        </div>
        <button type="button" className="admin-button" onClick={openCreate}>
          Yeni kullanıcı
        </button>
      </header>

      {error && <p className="admin-error">{error}</p>}

      {!isLoading && (
        <AdminSearch
          value={query}
          onChange={setQuery}
          label="Kullanıcı adı veya rol ara"
          shown={visibleUsers.length}
          total={users.length}
        />
      )}

      {isLoading ? (
        <p className="admin-empty">Yükleniyor…</p>
      ) : visibleUsers.length === 0 ? (
        <p className="admin-empty">
          {query ? 'Aramaya uyan kullanıcı yok.' : 'Henüz kullanıcı yok.'}
        </p>
      ) : (
        <table className="admin-table">
          <thead>
            <tr>
              <th>Kullanıcı</th>
              <th>Roller</th>
              <th>Durum</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {visibleUsers.map((user) => (
              <tr key={user.id}>
                <td>
                  <strong>{user.username}</strong>
                </td>
                <td>
                  {user.roleNames.length === 0 ? (
                    <span className="admin-chip admin-chip--muted">rol yok</span>
                  ) : (
                    user.roleNames.map((name) => (
                      <span key={name} className="admin-chip">
                        {name}
                      </span>
                    ))
                  )}
                </td>
                <td>
                  <span
                    className={user.isActive ? 'admin-status admin-status--on' : 'admin-status admin-status--off'}
                  >
                    {user.isActive ? 'Aktif' : 'Pasif'}
                  </span>
                </td>
                <td>
                  <div className="admin-table__actions">
                    {/* Coğrafi yetki düğmesi yalnızca yetkisi olana görünüyor. */}
                    {canManageGeo && (
                      <button
                        type="button"
                        onClick={() =>
                          setGeoTarget({ type: 'user', id: user.id, label: user.username })
                        }
                      >
                        Alan
                      </button>
                    )}
                    <button type="button" onClick={() => openAccess(user)}>
                      Yetkiler
                    </button>
                    <button type="button" onClick={() => openEdit(user)}>
                      Güncelle
                    </button>
                    <button type="button" className="is-danger" onClick={() => handleDelete(user)}>
                      Sil
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {geoTarget && (
        <GeoAreaDialog target={geoTarget} onClose={() => setGeoTarget(null)} />
      )}

      {/* --- Kullanıcı ekle / güncelle --- */}
      {form && (
        <div className="admin-modal__backdrop">
          <form className="admin-modal" onSubmit={handleSubmit}>
            <h2>{form.id === null ? 'Yeni kullanıcı' : 'Kullanıcıyı güncelle'}</h2>
            <p className="admin-modal__subtitle">
              Roller buradan atanır; ek yetkiler için "Yetkiler" ekranını kullanın.
            </p>

            <div className="admin-field">
              <label htmlFor="user-name">Kullanıcı adı</label>
              <input
                id="user-name"
                type="text"
                value={form.username}
                maxLength={50}
                onChange={(e) => setForm({ ...form, username: e.target.value })}
                autoFocus
              />
            </div>

            <div className="admin-field">
              <label htmlFor="user-pass">Şifre</label>
              <input
                id="user-pass"
                type="password"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
              />
              <span className="admin-field__hint">
                {form.id === null
                  ? 'En az 6 karakter.'
                  : 'Boş bırakırsanız mevcut şifre değişmez.'}
              </span>
            </div>

            <label className="admin-check">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              <span className="admin-check__text">
                <span>Aktif</span>
                <span className="admin-check__desc">Pasif kullanıcı giriş yapamaz.</span>
              </span>
            </label>

            <div className="admin-field">
              <label>Roller</label>
              {roles.map((role) => (
                <label key={role.id} className="admin-check">
                  <input
                    type="checkbox"
                    checked={form.roleIds.includes(role.id)}
                    onChange={() => toggleFormRole(role.id)}
                  />
                  <span className="admin-check__text">
                    <span>{role.name}</span>
                    <span className="admin-check__desc">{role.description}</span>
                  </span>
                </label>
              ))}
            </div>

            {formError && <p className="admin-error">{formError}</p>}

            <div className="admin-modal__actions">
              <button
                type="button"
                className="admin-modal__cancel"
                onClick={() => setForm(null)}
                disabled={isSaving}
              >
                Vazgeç
              </button>
              <button type="submit" className="admin-button" disabled={isSaving}>
                {isSaving ? 'Kaydediliyor…' : 'Kaydet'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* --- Yetki sekmesi --- */}
      {access && (
        <div className="admin-modal__backdrop">
          <div className="admin-modal">
            <h2>{access.username} — yetkiler</h2>
            <p className="admin-modal__subtitle">
              Rolden gelen yetkiler kilitlidir; kaldırmak için rolü değiştirin.
              Buradan yalnızca ek (doğrudan) yetki verirsiniz.
            </p>

            <div className="admin-field">
              <label>Roller</label>
              <div>
                {access.roleIds.length === 0 ? (
                  <span className="admin-chip admin-chip--muted">rol yok</span>
                ) : (
                  roles
                    .filter((role) => access.roleIds.includes(role.id))
                    .map((role) => (
                      <span key={role.id} className="admin-chip">
                        {role.name}
                      </span>
                    ))
                )}
              </div>
            </div>

            <div className="admin-field">
              <label>Yetkiler</label>
              {access.permissions.map((permission) => {
                const fromRole = permission.fromRoles.length > 0

                return (
                  <label
                    key={permission.id}
                    className={fromRole ? 'admin-check admin-check--locked' : 'admin-check'}
                  >
                    <input
                      type="checkbox"
                      // Rolden gelen yetki zaten var; tekrar seçtirmiyoruz.
                      checked={fromRole || permission.isDirect}
                      disabled={fromRole}
                      onChange={() => toggleDirectPermission(permission.id)}
                    />
                    <span className="admin-check__text">
                      <span>{permission.name}</span>
                      <span className="admin-check__desc">{permission.description}</span>
                      {fromRole && (
                        <span className="admin-check__source">
                          {permission.fromRoles.join(', ')} rolünden geliyor
                        </span>
                      )}
                    </span>
                  </label>
                )
              })}
            </div>

            {formError && <p className="admin-error">{formError}</p>}

            <div className="admin-modal__actions">
              <button
                type="button"
                className="admin-modal__cancel"
                onClick={() => setAccess(null)}
                disabled={isSaving}
              >
                Vazgeç
              </button>
              <button
                type="button"
                className="admin-button"
                onClick={handleSaveAccess}
                disabled={isSaving}
              >
                {isSaving ? 'Kaydediliyor…' : 'Kaydet'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
