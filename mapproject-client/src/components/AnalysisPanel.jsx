import './AnalysisPanel.css'

const TYPE_LABELS = {
  point: 'Nokta',
  line: 'Çizgi',
  polygon: 'Poligon',
}

/**
 * Kesişim analizinin sonucunu gösterir.
 * @param {string} title analizin kaynağı (kaydedilen poligon / geçici araç)
 * @param {boolean} isLoading
 * @param {object} [result] AnalysisResultDto
 * @param {string} [error]
 * @param {() => void} onClose panel + varsa geçici poligon temizlenir
 */
export default function AnalysisPanel({ title, isLoading, result, error, onClose }) {
  return (
    <section className="analysis-panel" aria-live="polite">
      <header className="analysis-panel__header">
        <h2 className="analysis-panel__title">{title}</h2>
        <button
          type="button"
          className="analysis-panel__close"
          onClick={onClose}
          aria-label="Analizi temizle"
        >
          ×
        </button>
      </header>

      {isLoading && <p className="analysis-panel__status">Hesaplanıyor…</p>}

      {error && (
        <p className="analysis-panel__status analysis-panel__status--error" role="alert">
          {error}
        </p>
      )}

      {result && (
        <>
          <p className="analysis-panel__total">
            <strong>{result.totalCount}</strong> envanter kesişiyor
          </p>

          <ul className="analysis-panel__counts">
            <li>
              <span>Nokta</span>
              <strong>{result.pointCount}</strong>
            </li>
            <li>
              <span>Çizgi</span>
              <strong>{result.lineCount}</strong>
            </li>
            <li>
              <span>Poligon</span>
              <strong>{result.polygonCount}</strong>
            </li>
          </ul>

          {result.items.length > 0 && (
            <ul className="analysis-panel__items">
              {result.items.map((item) => (
                <li key={`${item.type}-${item.id}`}>
                  <span className={`analysis-panel__badge analysis-panel__badge--${item.type}`}>
                    {TYPE_LABELS[item.type]}
                  </span>
                  {item.name}
                </li>
              ))}
            </ul>
          )}

          {result.totalCount === 0 && (
            <p className="analysis-panel__status">Bu alanda envanter yok.</p>
          )}
        </>
      )}

      <button type="button" className="analysis-panel__clear" onClick={onClose}>
        Temizle
      </button>
    </section>
  )
}
