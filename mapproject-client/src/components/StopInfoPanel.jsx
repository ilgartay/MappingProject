import './PoiInfoPanel.css'

/** "2026-08-26T20:39:35Z" -> "26.08.2026 23:39" */
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
 * Durağa tıklanınca açılan bilgi kutusu.
 *
 * POI panelinin biçimini paylaşıyor (aynı CSS): iki panel de haritanın
 * sağ üstünde aynı işi yapıyor, farklı görünmeleri için sebep yok.
 */
export default function StopInfoPanel({ stop, canManage, onDelete, onClose }) {
  return (
    <aside className="poi-panel" role="dialog" aria-label="Durak bilgisi">
      <header className="poi-panel__header">
        <h2>{stop.name}</h2>
        <button type="button" className="poi-panel__close" onClick={onClose} aria-label="Kapat">
          ×
        </button>
      </header>

      <dl className="poi-panel__list">
        <dt>Güzergah</dt>
        <dd>
          <span
            style={{
              display: 'inline-block',
              width: 10,
              height: 10,
              borderRadius: '50%',
              background: stop.routeColor,
              marginRight: 6,
            }}
          />
          {stop.routeName}
        </dd>

        <dt>Sıra</dt>
        <dd>{stop.order}. durak</dd>

        <dt>Eklenme</dt>
        <dd>{formatDate(stop.createdDate)}</dd>

        {stop.modifiedDate && (
          <>
            <dt>Güncelleme</dt>
            <dd>{formatDate(stop.modifiedDate)}</dd>
          </>
        )}

        <dt>Durum</dt>
        <dd>
          <span className={stop.isActive ? 'poi-panel__on' : 'poi-panel__off'}>
            {stop.isActive ? 'Aktif' : 'Pasif'}
          </span>
        </dd>
      </dl>

      {canManage && (
        <div className="poi-panel__actions">
          <button type="button" className="poi-panel__delete" onClick={onDelete}>
            Sil
          </button>
        </div>
      )}
    </aside>
  )
}
