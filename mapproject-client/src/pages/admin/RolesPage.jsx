import { useCallback, useEffect, useState } from 'react'
import {
  createRole,
  deleteRole,
  fetchPermissions,
  fetchRoles,
  updateRole,
} from '../../api/admin'
import { useAuth } from '../../auth/useAuth'
import GeoAreaDialog from './GeoAreaDialog'

const EMPTY_FORM = { name: '', description: '', isActive: true, permissionIds: [] }

export default function RolesPage() {
  const { hasPermission } = useAuth()
  const canManageGeo = hasPermission('geo.manage')

  const [geoTarget, setGeoTarget] = useState(null) // {type, id, label}
  const [roles, setRoles] = useState([])
  const [permissions, setPermissions] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  // null = kapalı, {id: null} = yeni, {id: 3, ...} = düzenleme
  const [form, setForm] = useState(null)
  const [isSaving, setIsSaving] = useState(false)
  const [formError, setFormError] = useState('')

  // Veri çekme ile state yazma ayrı: effect içinde doğrudan setState
  // zinciri kurmak cascading render'a yol açıyor (react-hooks kuralı).
  const fetchAll = useCallback(() => Promise.all([fetchRoles(), fetchPermissions()]), [])

  const applyData = useCallback(([roleList, permissionList]) => {
    setRoles(roleList)
    setPermissions(permissionList)
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
          setError('Roller yüklenemedi.')
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

  function openEdit(role) {
    setFormError('')
    setForm({
      id: role.id,
      name: role.name,
      description: role.description,
      isActive: role.isActive,
      permissionIds: [...role.permissionIds],
    })
  }

  function togglePermission(id) {
    setForm((current) => ({
      ...current,
      permissionIds: current.permissionIds.includes(id)
        ? current.permissionIds.filter((p) => p !== id)
        : [...current.permissionIds, id],
    }))
  }

  async function handleSubmit(event) {
    event.preventDefault()

    if (!form.name.trim()) {
      setFormError('Rol adı zorunludur.')
      return
    }

    setFormError('')
    setIsSaving(true)

    const payload = {
      name: form.name.trim(),
      description: form.description,
      isActive: form.isActive,
      permissionIds: form.permissionIds,
    }

    try {
      if (form.id === null) {
        await createRole(payload)
      } else {
        await updateRole(form.id, payload)
      }

      setForm(null)
      await load()
    } catch (err) {
      setFormError(err.response?.data?.message ?? 'Kaydedilemedi.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDelete(role) {
    const message =
      role.userCount > 0
        ? `"${role.name}" rolü ${role.userCount} kullanıcıda tanımlı. Silinsin mi?`
        : `"${role.name}" rolü silinsin mi?`

    if (!window.confirm(message)) return

    try {
      await deleteRole(role.id)
      await load()
    } catch (err) {
      if (err.response?.status !== 401) setError('Rol silinemedi.')
    }
  }

  return (
    <>
      <header className="admin-page__header">
        <div>
          <h1>Rol Listesi</h1>
          <p>Rolleri yönetin ve her role hangi yetkilerin verileceğini seçin.</p>
        </div>
        <button type="button" className="admin-button" onClick={openCreate}>
          Yeni rol
        </button>
      </header>

      {error && <p className="admin-error">{error}</p>}

      {isLoading ? (
        <p className="admin-empty">Yükleniyor…</p>
      ) : roles.length === 0 ? (
        <p className="admin-empty">Henüz rol yok.</p>
      ) : (
        <table className="admin-table">
          <thead>
            <tr>
              <th>Rol</th>
              <th>Yetkiler</th>
              <th>Kullanıcı</th>
              <th>Durum</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {roles.map((role) => (
              <tr key={role.id}>
                <td>
                  <strong>{role.name}</strong>
                  {role.description && (
                    <div className="admin-check__desc">{role.description}</div>
                  )}
                </td>
                <td>
                  {role.permissionIds.length === 0 ? (
                    <span className="admin-chip admin-chip--muted">yetki yok</span>
                  ) : (
                    <span className="admin-chip">{role.permissionIds.length} yetki</span>
                  )}
                </td>
                <td>{role.userCount}</td>
                <td>
                  <span
                    className={role.isActive ? 'admin-status admin-status--on' : 'admin-status admin-status--off'}
                  >
                    {role.isActive ? 'Aktif' : 'Pasif'}
                  </span>
                </td>
                <td>
                  <div className="admin-table__actions">
                    {canManageGeo && (
                      <button
                        type="button"
                        onClick={() =>
                          setGeoTarget({ type: 'role', id: role.id, label: role.name })
                        }
                      >
                        Alan
                      </button>
                    )}
                    <button type="button" onClick={() => openEdit(role)}>
                      Güncelle
                    </button>
                    <button type="button" className="is-danger" onClick={() => handleDelete(role)}>
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

      {form && (
        <div className="admin-modal__backdrop">
          <form className="admin-modal" onSubmit={handleSubmit}>
            <h2>{form.id === null ? 'Yeni rol' : 'Rolü güncelle'}</h2>
            <p className="admin-modal__subtitle">
              Buradan verilen yetkiler roldeki tüm kullanıcılara geçer.
            </p>

            <div className="admin-field">
              <label htmlFor="role-name">Rol adı</label>
              <input
                id="role-name"
                type="text"
                value={form.name}
                maxLength={50}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                autoFocus
              />
            </div>

            <div className="admin-field">
              <label htmlFor="role-desc">Açıklama</label>
              <input
                id="role-desc"
                type="text"
                value={form.description}
                maxLength={200}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
              />
            </div>

            <label className="admin-check">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              <span className="admin-check__text">
                <span>Aktif</span>
              </span>
            </label>

            <div className="admin-field">
              <label>Yetkiler</label>
              {permissions.map((permission) => (
                <label key={permission.id} className="admin-check">
                  <input
                    type="checkbox"
                    checked={form.permissionIds.includes(permission.id)}
                    onChange={() => togglePermission(permission.id)}
                  />
                  <span className="admin-check__text">
                    <span>{permission.name}</span>
                    <span className="admin-check__desc">{permission.description}</span>
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
    </>
  )
}
