import { useCallback, useEffect, useState } from 'react'
import {
  categoryLabel,
  createCategory,
  deleteCategory,
  deletePoi,
  fetchCategories,
  fetchCategoryTree,
  fetchPois,
  updateCategory,
} from '../../api/poi'
import { useAuth } from '../../auth/useAuth'
import AdminSearch from './AdminSearch'
import { matchesQuery } from './adminFilter'

const EMPTY_CATEGORY = { name: '', parentId: '', isActive: true }

/** "2026-08-22T20:39:35Z" -> "22.08.2026 23:39" */
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

/**
 * POI Yönetimi.
 *
 * İki bölüm var: üstte kategori ağacı (admin'in tanımladığı, operatörün
 * formda seçtiği liste), altta eklenen POI'ler ve kimin eklediği.
 * Tek sayfada durmaları bilinçli - kategori eklerken POI listesindeki
 * etkisini aynı ekranda görmek istiyorsun.
 */
export default function PoiPage() {
  const { hasPermission } = useAuth()
  const canManageCategories = hasPermission('category.manage')
  const canManagePois = hasPermission('poi.manage')

  const [pois, setPois] = useState([])
  const [tree, setTree] = useState([])
  const [flatCategories, setFlatCategories] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [query, setQuery] = useState('')

  const [form, setForm] = useState(null) // {id: null|number, ...}
  const [isSaving, setIsSaving] = useState(false)
  const [formError, setFormError] = useState('')

  // Veri çekme ile state yazma ayrı: effect içinde doğrudan setState
  // zinciri kurmak cascading render'a yol açıyor (react-hooks kuralı).
  const fetchAll = useCallback(
    () => Promise.all([fetchPois(), fetchCategoryTree(), fetchCategories()]),
    [],
  )

  const applyData = useCallback(([poiList, treeList, flatList]) => {
    setPois(poiList)
    setTree(treeList)
    setFlatCategories(flatList)
    setError('')
  }, [])

  const load = useCallback(() => fetchAll().then(applyData), [fetchAll, applyData])

  useEffect(() => {
    let cancelled = false

    fetchAll()
      .then((data) => {
        if (!cancelled) applyData(data)
      })
      .catch((err) => {
        if (!cancelled && err.response?.status !== 401) {
          setError('POI verileri yüklenemedi.')
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [fetchAll, applyData])

  // --- Kategori formu ---

  function openCreate() {
    setFormError('')
    setForm({ id: null, ...EMPTY_CATEGORY })
  }

  function openEdit(category) {
    setFormError('')
    setForm({
      id: category.id,
      name: category.name,
      parentId: category.parentId ?? '',
      isActive: category.isActive,
    })
  }

  async function handleSubmit(event) {
    event.preventDefault()

    if (!form.name.trim()) {
      setFormError('Kategori adı zorunludur.')
      return
    }

    setFormError('')
    setIsSaving(true)

    const payload = {
      name: form.name.trim(),
      // Boş seçim kök kategori demek; sunucu null bekliyor.
      parentId: form.parentId === '' ? null : Number(form.parentId),
      isActive: form.isActive,
    }

    try {
      if (form.id === null) {
        await createCategory(payload)
      } else {
        await updateCategory(form.id, payload)
      }

      setForm(null)
      await load()
    } catch (err) {
      setFormError(err.response?.data?.message ?? 'Kaydedilemedi.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDeleteCategory(category) {
    if (!window.confirm(`"${category.name}" kategorisi silinsin mi?`)) return

    try {
      await deleteCategory(category.id)
      await load()
    } catch (err) {
      if (err.response?.status !== 401) {
        setError(err.response?.data?.message ?? 'Kategori silinemedi.')
      }
    }
  }

  async function handleDeletePoi(poi) {
    if (!window.confirm(`"${poi.name}" POI'si silinsin mi?`)) return

    try {
      await deletePoi(poi.id)
      await load()
    } catch (err) {
      if (err.response?.status !== 401) setError("POI silinemedi.")
    }
  }

  // Ad, kategori yolu ve ekleyen kullanıcı birlikte taranıyor:
  // "demo" yazan o kullanıcının eklediklerini, "Kafe" yazan o
  // kategoridekileri görsün.
  const visiblePois = pois.filter((poi) =>
    matchesQuery(query, poi.name, poi.categoryPath, poi.userName),
  )

  /** Ağacı girintili satırlara açar; her seviye bir adım içeri kayar. */
  function renderCategoryRows(nodes, depth = 0) {
    return nodes.flatMap((node) => [
      <tr key={node.id}>
        <td>
          <span style={{ paddingLeft: `${depth * 20}px` }}>
            {depth > 0 && <span className="poi-admin__branch">└</span>}
            <strong>{node.name}</strong>
          </span>
        </td>
        <td>
          {node.poiCount === 0 ? (
            <span className="admin-chip admin-chip--muted">POI yok</span>
          ) : (
            <span className="admin-chip">{node.poiCount} POI</span>
          )}
        </td>
        <td>
          <span
            className={node.isActive ? 'admin-status admin-status--on' : 'admin-status admin-status--off'}
          >
            {node.isActive ? 'Aktif' : 'Pasif'}
          </span>
        </td>
        <td>
          {canManageCategories && (
            <div className="admin-table__actions">
              <button type="button" onClick={() => openEdit(node)}>
                Güncelle
              </button>
              <button type="button" className="is-danger" onClick={() => handleDeleteCategory(node)}>
                Sil
              </button>
            </div>
          )}
        </td>
      </tr>,
      ...renderCategoryRows(node.children, depth + 1),
    ])
  }

  return (
    <>
      <header className="admin-page__header">
        <div>
          <h1>POI Yönetimi</h1>
          <p>Kategori ağacını düzenleyin, eklenen ilgi noktalarını görüntüleyin.</p>
        </div>
        {canManageCategories && (
          <button type="button" className="admin-button" onClick={openCreate}>
            Yeni kategori
          </button>
        )}
      </header>

      {error && <p className="admin-error">{error}</p>}

      {isLoading ? (
        <p className="admin-empty">Yükleniyor…</p>
      ) : (
        <>
          <h2 className="poi-admin__title">Kategoriler</h2>

          {tree.length === 0 ? (
            <p className="admin-empty">Henüz kategori yok.</p>
          ) : (
            <table className="admin-table">
              <thead>
                <tr>
                  <th>Kategori</th>
                  <th>POI</th>
                  <th>Durum</th>
                  <th />
                </tr>
              </thead>
              <tbody>{renderCategoryRows(tree)}</tbody>
            </table>
          )}

          <h2 className="poi-admin__title">İlgi Noktaları</h2>

          {pois.length > 0 && (
            <AdminSearch
              value={query}
              onChange={setQuery}
              label="POI adı, kategori veya ekleyen kullanıcı ara"
              shown={visiblePois.length}
              total={pois.length}
            />
          )}

          {visiblePois.length === 0 ? (
            <p className="admin-empty">
              {query ? 'Aramaya uyan POI yok.' : 'Henüz POI eklenmemiş.'}
            </p>
          ) : (
            <table className="admin-table">
              <thead>
                <tr>
                  <th>İsim</th>
                  <th>Kategori</th>
                  <th>Mesai saatleri</th>
                  <th>Ekleyen</th>
                  <th>Eklenme</th>
                  <th>Durum</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {visiblePois.map((poi) => (
                  <tr key={poi.id}>
                    <td>
                      <strong>{poi.name}</strong>
                    </td>
                    <td>
                      <span className="admin-chip">{poi.categoryPath || poi.categoryName}</span>
                    </td>
                    <td>{poi.workingHours || '—'}</td>
                    <td>{poi.userName}</td>
                    <td>{formatDate(poi.createdDate)}</td>
                    <td>
                      <span
                        className={poi.isActive ? 'admin-status admin-status--on' : 'admin-status admin-status--off'}
                      >
                        {poi.isActive ? 'Aktif' : 'Pasif'}
                      </span>
                    </td>
                    <td>
                      {canManagePois && (
                        <div className="admin-table__actions">
                          <button type="button" className="is-danger" onClick={() => handleDeletePoi(poi)}>
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

      {/* --- Kategori ekle / güncelle --- */}
      {form && (
        <div className="admin-modal__backdrop">
          <form className="admin-modal" onSubmit={handleSubmit}>
            <h2>{form.id === null ? 'Yeni kategori' : 'Kategoriyi güncelle'}</h2>
            <p className="admin-modal__subtitle">
              Üst kategori seçilmezse kök kategori olarak eklenir.
            </p>

            <div className="admin-field">
              <label htmlFor="category-name">Kategori adı</label>
              <input
                id="category-name"
                type="text"
                value={form.name}
                maxLength={100}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                autoFocus
              />
            </div>

            <div className="admin-field">
              <label htmlFor="category-parent">Üst kategori</label>
              <select
                id="category-parent"
                value={form.parentId}
                onChange={(e) => setForm({ ...form, parentId: e.target.value })}
              >
                <option value="">(kök kategori)</option>
                {flatCategories
                  // Kategori kendi üstü olamaz; sunucu da reddediyor ama
                  // listede göstermemek daha anlaşılır.
                  .filter((c) => c.id !== form.id)
                  .map((category) => (
                    <option key={category.id} value={category.id}>
                      {categoryLabel(category)}
                    </option>
                  ))}
              </select>
            </div>

            <label className="admin-check">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              <span>Aktif (pasif kategoriler POI formunda seçilemez)</span>
            </label>

            {formError && <p className="admin-error">{formError}</p>}

            <div className="admin-modal__actions">
              <button type="button" onClick={() => setForm(null)}>
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
