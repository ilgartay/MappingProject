import { useCallback, useEffect, useState } from 'react'
import {
  deleteRoute,
  deleteStop,
  fetchRoutes,
  ROUTE_COLORS,
  updateRoute,
  updateStop,
} from '../../api/transport'
import { useAuth } from '../../auth/useAuth'
import AdminSearch from './AdminSearch'
import { matchesQuery } from './adminFilter'

/** "2026-08-30T10:39:35Z" -> "30.08.2026 13:39" */
function formatDate(value) {
  if (!value) return '—'

  return new Date(value).toLocaleString('tr-TR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** 18320 -> "18,3 km" */
function formatDistance(metres) {
  return `${(metres / 1000).toLocaleString('tr-TR', { maximumFractionDigits: 1 })} km`
}

/**
 * Ulaşım Yönetimi: güzergahlar ve duraklar.
 *
 * Haritadaki Güzergah Yönetimi paneliyle çakışmıyor, onu tamamlıyor.
 * Panel operatörün harita başında yaptığı iş için (durak koyma, sıralama,
 * rota üretme); bu ekran toplu bakım için - hangi hatta kaç durak var,
 * hangisinin rotası üretilmiş, yanlış girilmiş bir adı düzeltmek.
 *
 * Durağın konumu burada değiştirilemiyor: bir noktayı koordinat yazarak
 * taşımak haritadan sürüklemekten hem zor hem hataya açık. Ad, güzergah
 * ve durum düzenlenebiliyor.
 */
export default function TransportPage() {
  const { hasPermission } = useAuth()
  const canManageRoutes = hasPermission('route.manage')
  const canManageStops = hasPermission('stop.manage')

  const [routes, setRoutes] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [routeQuery, setRouteQuery] = useState('')
  const [stopQuery, setStopQuery] = useState('')

  // Düzenlenen satır. kind alanı formun hangi tabloya ait olduğunu
  // söylüyor; iki tablo için ayrı iki modal yazmaya değmiyor.
  const [form, setForm] = useState(null)
  const [isSaving, setIsSaving] = useState(false)
  const [formError, setFormError] = useState('')

  const load = useCallback(() => fetchRoutes().then(setRoutes), [])

  useEffect(() => {
    let cancelled = false

    fetchRoutes()
      .then((list) => {
        if (!cancelled) {
          setRoutes(list)
          setError('')
        }
      })
      .catch((err) => {
        if (!cancelled && err.response?.status !== 401) {
          setError('Ulaşım verileri yüklenemedi.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [])

  // Duraklar güzergahın içinde geliyor; tablo için düz listeye açıyoruz.
  const stops = routes.flatMap((route) =>
    route.stops.map((stop) => ({ ...stop, routeColor: route.color })),
  )

  const visibleRoutes = routes.filter((route) => matchesQuery(routeQuery, route.name))
  const visibleStops = stops.filter((stop) => matchesQuery(stopQuery, stop.name, stop.routeName))

  async function handleSubmit(event) {
    event.preventDefault()

    if (!form.name.trim()) {
      setFormError('Ad boş olamaz.')
      return
    }

    setIsSaving(true)

    try {
      if (form.kind === 'route') {
        await updateRoute(form.id, {
          name: form.name.trim(),
          color: form.color,
          isActive: form.isActive,
        })
      } else {
        // Konum bu ekranda değişmiyor; WKT olduğu gibi geri gidiyor.
        await updateStop(form.id, {
          name: form.name.trim(),
          wkt: form.wkt,
          routeId: form.routeId,
          isActive: form.isActive,
        })
      }

      setForm(null)
      setFormError('')
      await load()
    } catch (err) {
      setFormError(err.response?.data?.message ?? 'Kaydedilemedi.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDeleteRoute(route) {
    if (!window.confirm(`"${route.name}" güzergahı silinsin mi?`)) return

    try {
      await deleteRoute(route.id)
      setError('')
      await load()
    } catch (err) {
      if (err.response?.status !== 401) {
        setError(err.response?.data?.message ?? 'Güzergah silinemedi.')
      }
    }
  }

  async function handleDeleteStop(stop) {
    if (!window.confirm(`"${stop.name}" durağı silinsin mi?`)) return

    try {
      await deleteStop(stop.id)
      setError('')
      await load()
    } catch (err) {
      if (err.response?.status !== 401) {
        setError(err.response?.data?.message ?? 'Durak silinemedi.')
      }
    }
  }

  return (
    <>
      <header className="admin-page__header">
        <div>
          <h1>Ulaşım Yönetimi</h1>
          <p>Güzergahların adını ve rengini düzenleyin, durakları yönetin.</p>
        </div>
      </header>

      {error && <p className="admin-error">{error}</p>}

      {isLoading ? (
        <p className="admin-empty">Yükleniyor…</p>
      ) : (
        <>
          <h2 className="poi-admin__title">Güzergahlar</h2>

          {routes.length > 0 && (
            <AdminSearch
              value={routeQuery}
              onChange={setRouteQuery}
              label="Güzergah adı ara"
              shown={visibleRoutes.length}
              total={routes.length}
            />
          )}

          {visibleRoutes.length === 0 ? (
            <p className="admin-empty">
              {routeQuery ? 'Aramaya uyan güzergah yok.' : 'Henüz güzergah yok.'}
            </p>
          ) : (
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Ad</th>
                  <th>Renk</th>
                  <th>Durak</th>
                  <th>Rota</th>
                  <th>Rota tarihi</th>
                  <th>Durum</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {visibleRoutes.map((route) => (
                  <tr key={route.id}>
                    <td>
                      <strong>{route.name}</strong>
                    </td>
                    <td>
                      <span className="admin-swatch" style={{ background: route.color }} />
                      {route.color}
                    </td>
                    <td>
                      {route.stops.length === 0 ? (
                        <span className="admin-chip admin-chip--muted">durak yok</span>
                      ) : (
                        <span className="admin-chip">{route.stops.length} durak</span>
                      )}
                    </td>
                    <td>
                      {route.routeWkt ? (
                        <span className="admin-chip">{formatDistance(route.routeDistance)}</span>
                      ) : (
                        <span className="admin-chip admin-chip--muted">üretilmedi</span>
                      )}
                    </td>
                    <td>{formatDate(route.routeBuiltAt)}</td>
                    <td>
                      <span
                        className={route.isActive ? 'admin-status admin-status--on' : 'admin-status admin-status--off'}
                      >
                        {route.isActive ? 'Aktif' : 'Pasif'}
                      </span>
                    </td>
                    <td>
                      {canManageRoutes && (
                        <div className="admin-table__actions">
                          <button
                            type="button"
                            onClick={() =>
                              setForm({
                                kind: 'route',
                                id: route.id,
                                name: route.name,
                                color: route.color,
                                isActive: route.isActive,
                              })
                            }
                          >
                            Güncelle
                          </button>
                          <button
                            type="button"
                            className="is-danger"
                            onClick={() => handleDeleteRoute(route)}
                          >
                            Sil
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          <h2 className="poi-admin__title">Duraklar</h2>

          {stops.length > 0 && (
            <AdminSearch
              value={stopQuery}
              onChange={setStopQuery}
              label="Durak adı veya güzergah ara"
              shown={visibleStops.length}
              total={stops.length}
            />
          )}

          {visibleStops.length === 0 ? (
            <p className="admin-empty">
              {stopQuery ? 'Aramaya uyan durak yok.' : 'Henüz durak yok.'}
            </p>
          ) : (
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Ad</th>
                  <th>Güzergah</th>
                  <th>Sıra</th>
                  <th>Eklenme</th>
                  <th>Durum</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {visibleStops.map((stop) => (
                  <tr key={stop.id}>
                    <td>
                      <strong>{stop.name}</strong>
                    </td>
                    <td>
                      <span className="admin-swatch" style={{ background: stop.routeColor }} />
                      {stop.routeName}
                    </td>
                    <td>{stop.order}</td>
                    <td>{formatDate(stop.createdDate)}</td>
                    <td>
                      <span
                        className={stop.isActive ? 'admin-status admin-status--on' : 'admin-status admin-status--off'}
                      >
                        {stop.isActive ? 'Aktif' : 'Pasif'}
                      </span>
                    </td>
                    <td>
                      {canManageStops && (
                        <div className="admin-table__actions">
                          <button
                            type="button"
                            onClick={() =>
                              setForm({
                                kind: 'stop',
                                id: stop.id,
                                name: stop.name,
                                wkt: stop.wkt,
                                routeId: stop.routeId,
                                isActive: stop.isActive,
                              })
                            }
                          >
                            Güncelle
                          </button>
                          <button
                            type="button"
                            className="is-danger"
                            onClick={() => handleDeleteStop(stop)}
                          >
                            Sil
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </>
      )}

      {/* --- Düzenleme penceresi --- */}
      {form && (
        <div className="admin-modal__backdrop">
          <form className="admin-modal" onSubmit={handleSubmit}>
            <h2>{form.kind === 'route' ? 'Güzergahı güncelle' : 'Durağı güncelle'}</h2>
            <p className="admin-modal__subtitle">
              {form.kind === 'route'
                ? 'Renk hem hattı hem duraklarını haritada boyuyor.'
                : 'Durağın konumu haritadan değiştirilir; burada adı ve güzergahı düzenlenir.'}
            </p>

            <div className="admin-field">
              <label htmlFor="transport-name">Ad</label>
              <input
                id="transport-name"
                type="text"
                value={form.name}
                maxLength={100}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                autoFocus
              />
            </div>

            {form.kind === 'route' && (
              <div className="admin-field">
                <span>Renk</span>
                <div className="admin-colors">
                  {ROUTE_COLORS.map((color) => (
                    <button
                      key={color}
                      type="button"
                      className={
                        color === form.color ? 'admin-swatch admin-swatch--on' : 'admin-swatch'
                      }
                      style={{ background: color }}
                      aria-label={`Renk ${color}`}
                      aria-pressed={color === form.color}
                      onClick={() => setForm({ ...form, color })}
                    />
                  ))}
                </div>
              </div>
            )}

            {form.kind === 'stop' && (
              <div className="admin-field">
                <label htmlFor="transport-route">Güzergah</label>
                <select
                  id="transport-route"
                  value={form.routeId}
                  onChange={(e) => setForm({ ...form, routeId: Number(e.target.value) })}
                >
                  {routes.map((route) => (
                    <option key={route.id} value={route.id}>
                      {route.name}
                    </option>
                  ))}
                </select>
              </div>
            )}

            <label className="admin-check">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              <span>Aktif</span>
            </label>

            {formError && <p className="admin-error">{formError}</p>}

            <div className="admin-modal__actions">
              <button
                type="button"
                onClick={() => {
                  setForm(null)
                  setFormError('')
                }}
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
