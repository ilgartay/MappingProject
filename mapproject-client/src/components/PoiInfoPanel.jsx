import './PoiInfoPanel.css'

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
 * POI'ye tıklanınca açılan bilgi paneli.
 *
 * Salt okunur: POI'ler paylaşılan veri, düzenleme admin panelinden
 * yapılıyor. Operatörün kendi eklediğini haritadan değiştirmesine
 * izin verseydik başkasının kaydını da değiştirebilmesi gerekirdi.
 */
export default function PoiInfoPanel({ poi, onClose }) {
  return (
    <aside className="poi-panel" role="dialog" aria-label="POI bilgisi">
      <header className="poi-panel__header">
        <h2>{poi.name}</h2>
        <button type="button" className="poi-panel__close" onClick={onClose} aria-label="Kapat">
          ×
        </button>
      </header>

      <dl className="poi-panel__list">
        <dt>Kategori</dt>
        <dd>{poi.categoryPath || poi.categoryName}</dd>

        <dt>Mesai saatleri</dt>
        <dd>{poi.workingHours || '—'}</dd>

        <dt>Ekleyen</dt>
        <dd>{poi.userName}</dd>

        <dt>Eklenme</dt>
        <dd>{formatDate(poi.createdDate)}</dd>

        {poi.modifiedDate && (
          <>
            <dt>Güncelleme</dt>
            <dd>{formatDate(poi.modifiedDate)}</dd>
          </>
        )}

        <dt>Durum</dt>
        <dd>
          <span className={poi.isActive ? 'poi-panel__on' : 'poi-panel__off'}>
            {poi.isActive ? 'Aktif' : 'Pasif'}
          </span>
        </dd>
      </dl>
    </aside>
  )
}
