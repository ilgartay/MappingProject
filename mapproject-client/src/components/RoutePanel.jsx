import { useRef, useState } from 'react'
import { ROUTE_COLORS } from '../api/transport'
import './RoutePanel.css'

const EMPTY_ROUTE = { id: null, name: '', color: ROUTE_COLORS[0], isActive: true }

/** 18320 -> "18,3 km" */
function formatDistance(metres) {
  return `${(metres / 1000).toLocaleString('tr-TR', { maximumFractionDigits: 1 })} km`
}

/** 4260 -> "1 sa 11 dk" */
function formatDuration(seconds) {
  const total = Math.round(seconds / 60)
  const hours = Math.floor(total / 60)
  const minutes = total % 60
  return hours > 0 ? `${hours} sa ${minutes} dk` : `${minutes} dk`
}

/**
 * Güzergah Yönetimi paneli.
 *
 * Admin panelinde değil haritanın üstünde duruyor. İki sebep: durakların
 * sırası haritada anında görünmeli, ve Ulaşım Operatörü rolünün admin
 * paneline girecek yetkisi yok (user.manage / role.manage taşımıyor) -
 * paneli oraya koysaydık asıl kullanıcısı erişemezdi.
 *
 * Sürükle-bırak tarayıcının kendi HTML5 drag olaylarıyla; küçük bir
 * liste için ayrı bir kütüphane eklemeye değmiyor.
 */
export default function RoutePanel({
  routes,
  selectedRouteId,
  canManage,
  isAddingStop,
  isBuildingRoute,
  hiddenRouteIds,
  error,
  onSelectRoute,
  onSaveRoute,
  onDeleteRoute,
  onToggleAddStop,
  onToggleRouteVisible,
  onBuildRoute,
  onReorder,
  onDeleteStop,
  onFocusStop,
  onClose,
}) {
  const [form, setForm] = useState(null)

  // Sürüklenen satırın numarası iki yerde tutuluyor:
  //
  // - ref: bırakma hesabı bunu kullanıyor. State'e koysaydık, dragstart
  //   ve drop aynı iş parçacığında tetiklendiğinde (bazı tarayıcılar,
  //   dokunmatik ve testler böyle yapıyor) React henüz yeniden
  //   render etmemiş olur ve hesap eski değerle çalışırdı.
  // - state: yalnızca sürüklenen satırı soluklaştırmak için; görsel
  //   geri bildirimin bir render beklemesi sorun değil.
  const dragIndexRef = useRef(null)
  const [dragIndex, setDragIndex] = useState(null)

  function startDrag(index) {
    dragIndexRef.current = index
    setDragIndex(index)
  }

  function endDrag() {
    dragIndexRef.current = null
    setDragIndex(null)
  }

  const selected = routes.find((r) => r.id === selectedRouteId) ?? null

  function handleSubmit(event) {
    event.preventDefault()
    if (!form.name.trim()) return
    onSaveRoute(form).then(() => setForm(null))
  }

  /** Sürüklenen satırı hedefin yerine taşıyıp yeni sırayı gönderiyor. */
  function handleDrop(targetIndex) {
    const sourceIndex = dragIndexRef.current

    if (sourceIndex === null || sourceIndex === targetIndex) {
      endDrag()
      return
    }

    const ids = selected.stops.map((s) => s.id)
    const [moved] = ids.splice(sourceIndex, 1)
    ids.splice(targetIndex, 0, moved)

    endDrag()
    onReorder(selected.id, ids)
  }

  return (
    <aside className="route-panel" role="dialog" aria-label="Güzergah yönetimi">
      <header className="route-panel__header">
        <h2>Güzergah Yönetimi</h2>
        <button type="button" className="route-panel__close" onClick={onClose} aria-label="Kapat">
          ×
        </button>
      </header>

      {error && <p className="route-panel__error" role="alert">{error}</p>}

      {/* --- Güzergah listesi --- */}
      <ul className="route-panel__routes">
        {routes.length === 0 && <li className="route-panel__empty">Henüz güzergah yok.</li>}

        {routes.map((route) => (
          <li key={route.id} className="route-panel__row">
            {/* Katman kontrolü: hattı haritadan kaldırır ama listede
                bırakır. Onay kutusu görünür duruyor - göz simgesi gibi
                bir şeyin açık mı kapalı mı olduğu tahmin gerektiriyor. */}
            <input
              type="checkbox"
              className="route-panel__toggle"
              id={`route-visible-${route.id}`}
              checked={!hiddenRouteIds.has(route.id)}
              onChange={() => onToggleRouteVisible(route.id)}
            />
            <label className="route-panel__sr-only" htmlFor={`route-visible-${route.id}`}>
              {route.name} haritada görünsün
            </label>

            <button
              type="button"
              className={
                route.id === selectedRouteId
                  ? 'route-panel__route route-panel__route--on'
                  : 'route-panel__route'
              }
              onClick={() => onSelectRoute(route.id === selectedRouteId ? null : route.id)}
            >
              <span className="route-panel__dot" style={{ background: route.color }} />
              <span className="route-panel__name">{route.name}</span>
              <span className="route-panel__count">{route.stops.length} durak</span>
            </button>
          </li>
        ))}
      </ul>

      {canManage && !form && (
        <button
          type="button"
          className="route-panel__add"
          onClick={() => setForm({ ...EMPTY_ROUTE })}
        >
          + Yeni güzergah
        </button>
      )}

      {/* --- Güzergah ekle / düzenle --- */}
      {form && (
        <form className="route-panel__form" onSubmit={handleSubmit}>
          <label className="route-panel__label" htmlFor="route-name">
            Güzergah adı
          </label>
          <input
            id="route-name"
            value={form.name}
            maxLength={100}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            placeholder="Örn. 100 - Kızılay–Keçiören"
            autoFocus
          />

          <span className="route-panel__label">Renk</span>
          <div className="route-panel__colors">
            {ROUTE_COLORS.map((color) => (
              <button
                key={color}
                type="button"
                className={
                  color === form.color
                    ? 'route-panel__swatch route-panel__swatch--on'
                    : 'route-panel__swatch'
                }
                style={{ background: color }}
                aria-label={`Renk ${color}`}
                aria-pressed={color === form.color}
                onClick={() => setForm({ ...form, color })}
              />
            ))}
          </div>

          <div className="route-panel__form-actions">
            <button type="button" onClick={() => setForm(null)}>
              Vazgeç
            </button>
            <button type="submit" className="route-panel__save">
              Kaydet
            </button>
          </div>
        </form>
      )}

      {/* --- Seçili güzergahın durakları --- */}
      {selected && (
        <>
          <div className="route-panel__section">
            <span>Duraklar</span>
            {canManage && (
              <button type="button" className="route-panel__edit" onClick={() => setForm(selected)}>
                Düzenle
              </button>
            )}
          </div>

          {selected.stops.length === 0 ? (
            <p className="route-panel__empty">
              Bu güzergahta durak yok. Aşağıdaki düğmeyle haritaya durak ekleyin.
            </p>
          ) : (
            <ol className="route-panel__stops">
              {selected.stops.map((stop, index) => (
                <li
                  key={stop.id}
                  draggable={canManage}
                  className={dragIndex === index ? 'route-panel__stop route-panel__stop--drag' : 'route-panel__stop'}
                  onDragStart={() => startDrag(index)}
                  onDragOver={(e) => e.preventDefault()}
                  onDrop={() => handleDrop(index)}
                  onDragEnd={endDrag}
                >
                  {canManage && <span className="route-panel__grip" aria-hidden="true">⠿</span>}
                  <span className="route-panel__order">{stop.order}</span>
                  <button
                    type="button"
                    className="route-panel__stop-name"
                    onClick={() => onFocusStop(stop)}
                  >
                    {stop.name}
                  </button>
                  {canManage && (
                    <button
                      type="button"
                      className="route-panel__remove"
                      onClick={() => onDeleteStop(stop)}
                      aria-label={`${stop.name} durağını sil`}
                    >
                      ×
                    </button>
                  )}
                </li>
              ))}
            </ol>
          )}

          {/* --- OSRM rotası --- */}
          <div className="route-panel__route-info">
            {selected.routeWkt ? (
              <p className="route-panel__route-stats">
                <strong>Rota:</strong> {formatDistance(selected.routeDistance)} ·{' '}
                {formatDuration(selected.routeDuration)}
              </p>
            ) : (
              <p className="route-panel__route-stats route-panel__route-stats--empty">
                Rota üretilmedi. Duraklar kesikli çizgiyle bağlı.
              </p>
            )}

            {canManage && (
              <button
                type="button"
                className="route-panel__build"
                disabled={isBuildingRoute || selected.stops.length < 2}
                onClick={() => onBuildRoute(selected.id)}
              >
                {isBuildingRoute
                  ? 'Hesaplanıyor…'
                  : selected.routeWkt
                    ? 'Rotayı yenile'
                    : 'Rota Oluştur'}
              </button>
            )}

            {canManage && selected.stops.length < 2 && (
              <p className="route-panel__hint">Rota için en az iki durak gerekiyor.</p>
            )}
          </div>

          {canManage && (
            <>
              <button
                type="button"
                className={
                  isAddingStop ? 'route-panel__draw route-panel__draw--on' : 'route-panel__draw'
                }
                aria-pressed={isAddingStop}
                onClick={onToggleAddStop}
              >
                {isAddingStop ? 'Durak eklemeyi bırak' : 'Haritaya durak ekle'}
              </button>

              {isAddingStop && (
                <p className="route-panel__hint">
                  Haritada bir noktaya tıklayın; nokta doğrudan bu güzergaha durak olarak eklenecek.
                </p>
              )}

              <button
                type="button"
                className="route-panel__delete"
                onClick={() => onDeleteRoute(selected)}
              >
                Güzergahı sil
              </button>
            </>
          )}
        </>
      )}
    </aside>
  )
}
