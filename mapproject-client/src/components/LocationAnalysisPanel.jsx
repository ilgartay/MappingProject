import { categoryLabel } from '../api/poi'
import { CRITERIA_RULES, validateAnalysis } from '../api/location'
import './LocationAnalysisPanel.css'

/** Puanı 100'e tamamlamak kolay olsun diye boş satırın varsayılanı. */
const NEW_CRITERION = { categoryId: '', weight: 0 }

/**
 * Konum analizi paneli.
 *
 * İki karar var: hedef bölge (il listesinden ya da haritaya çizerek) ve
 * kriterler (2-5 kategori, puanları toplamı tam 100). Toplam tutmadan
 * "Analizi başlat" açılmıyor - aynı kural sunucuda da var, buradaki
 * kullanıcı boşuna istek atmasın diye.
 */
export default function LocationAnalysisPanel({
  categories,
  provinces,
  provinceId,
  areaWkt,
  isDrawing,
  criteria,
  isRunning,
  error,
  onProvinceChange,
  onDrawToggle,
  onCriteriaChange,
  onRun,
  onClear,
  onClose,
}) {
  const filled = criteria.filter((c) => c.categoryId)
  const total = filled.reduce((sum, c) => sum + (Number(c.weight) || 0), 0)
  const problem = validateAnalysis({ criteria, provinceId, areaWkt })
  const selectable = categories.filter((c) => c.isActive && c.parentId)

  function updateRow(index, patch) {
    onCriteriaChange(criteria.map((row, i) => (i === index ? { ...row, ...patch } : row)))
  }

  function addRow() {
    if (criteria.length < CRITERIA_RULES.max) {
      onCriteriaChange([...criteria, { ...NEW_CRITERION }])
    }
  }

  function removeRow(index) {
    if (criteria.length > CRITERIA_RULES.min) {
      onCriteriaChange(criteria.filter((_, i) => i !== index))
    }
  }

  /** Kalan puanı son satıra vererek toplamı 100'e tamamlar. */
  function balance() {
    if (filled.length === 0) return

    const lastIndex = criteria.map((c) => Boolean(c.categoryId)).lastIndexOf(true)
    const others = criteria.reduce(
      (sum, c, i) => (c.categoryId && i !== lastIndex ? sum + (Number(c.weight) || 0) : sum),
      0,
    )

    updateRow(lastIndex, { weight: Math.max(0, CRITERIA_RULES.total - others) })
  }

  return (
    <aside className="loc-panel" role="dialog" aria-label="Konum analizi">
      <header className="loc-panel__header">
        <h2>Konum Analizi</h2>
        <button type="button" className="loc-panel__close" onClick={onClose} aria-label="Kapat">
          ×
        </button>
      </header>

      {/* --- Hedef bölge --- */}
      <p className="loc-panel__section">Hedef bölge</p>

      <select
        className="loc-panel__select"
        value={provinceId ?? ''}
        onChange={(e) => onProvinceChange(e.target.value ? Number(e.target.value) : null)}
        aria-label="İl seç"
      >
        <option value="">İl seçin…</option>
        {provinces.map((province) => (
          <option key={province.id} value={province.id}>
            {province.name}
          </option>
        ))}
      </select>

      <button
        type="button"
        className={isDrawing ? 'loc-panel__draw loc-panel__draw--on' : 'loc-panel__draw'}
        aria-pressed={isDrawing}
        onClick={onDrawToggle}
      >
        {isDrawing ? 'Çizimi bırak' : areaWkt ? 'Alanı yeniden çiz' : 'Haritada alan çiz'}
      </button>

      {areaWkt && !provinceId && (
        <p className="loc-panel__hint">Haritaya çizilen alan kullanılacak.</p>
      )}

      {/* --- Kriterler --- */}
      <p className="loc-panel__section">
        Kriterler
        <span className="loc-panel__count">
          {filled.length} / {CRITERIA_RULES.max}
        </span>
      </p>

      <ul className="loc-panel__criteria">
        {criteria.map((row, index) => (
          <li key={index}>
            <select
              value={row.categoryId}
              onChange={(e) => updateRow(index, { categoryId: e.target.value })}
              aria-label={`Kriter ${index + 1} kategorisi`}
            >
              <option value="">Kategori…</option>
              {selectable.map((category) => (
                <option key={category.id} value={category.id}>
                  {categoryLabel(category)}
                </option>
              ))}
            </select>

            <input
              type="number"
              min={0}
              max={100}
              value={row.weight}
              onChange={(e) => updateRow(index, { weight: Number(e.target.value) })}
              aria-label={`Kriter ${index + 1} puanı`}
            />

            <button
              type="button"
              className="loc-panel__remove"
              onClick={() => removeRow(index)}
              disabled={criteria.length <= CRITERIA_RULES.min}
              aria-label={`Kriter ${index + 1} satırını kaldır`}
            >
              −
            </button>
          </li>
        ))}
      </ul>

      <div className="loc-panel__row">
        <button
          type="button"
          className="loc-panel__add"
          onClick={addRow}
          disabled={criteria.length >= CRITERIA_RULES.max}
        >
          + Kriter ekle
        </button>

        <span className={total === CRITERIA_RULES.total ? 'loc-panel__total loc-panel__total--ok' : 'loc-panel__total'}>
          Toplam {total} / {CRITERIA_RULES.total}
        </span>
      </div>

      {total !== CRITERIA_RULES.total && filled.length > 0 && (
        <button type="button" className="loc-panel__balance" onClick={balance}>
          Kalanı son kritere ver
        </button>
      )}

      {problem && <p className="loc-panel__problem">{problem}</p>}
      {error && <p className="loc-panel__error" role="alert">{error}</p>}

      <div className="loc-panel__actions">
        <button type="button" className="loc-panel__clear" onClick={onClear}>
          Temizle
        </button>
        <button
          type="button"
          className="loc-panel__run"
          onClick={onRun}
          disabled={Boolean(problem) || isRunning}
        >
          {isRunning ? 'Hesaplanıyor…' : 'Analizi başlat'}
        </button>
      </div>
    </aside>
  )
}
