import './DrawToolbar.css'

const TOOLS = [
  {
    type: 'Point',
    label: 'Nokta',
    countKey: 'points',
    hint: 'Haritaya tıklayarak nokta ekleyin.',
    icon: (
      <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="12" cy="12" r="4" fill="currentColor" stroke="none" />
        <circle cx="12" cy="12" r="8" />
      </svg>
    ),
  },
  {
    type: 'LineString',
    label: 'Çizgi',
    countKey: 'lines',
    hint: 'Her tıklama bir kırılma noktası ekler, çift tıklayarak bitirin.',
    icon: (
      <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M4 18 L10 8 L16 14 L20 6" />
        <circle cx="4" cy="18" r="2" fill="currentColor" stroke="none" />
        <circle cx="20" cy="6" r="2" fill="currentColor" stroke="none" />
      </svg>
    ),
  },
  {
    type: 'Polygon',
    label: 'Poligon',
    countKey: 'polygons',
    hint: 'Köşeleri tıklayın, çift tıklayarak alanı kapatın.',
    icon: (
      <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M12 3 L21 10 L17 20 L7 20 L3 10 Z" />
      </svg>
    ),
  },
]

// Analiz aracı çizim araçlarından ayrı duruyor: geometri kaydetmiyor,
// sadece geçici bir poligonla sorgu yapıyor.
const ANALYSIS_TOOL = {
  type: 'Analysis',
  label: 'Envanter Analizi',
  // Dar ekranda tam etiket sığmıyor; ikon tek başına da anlaşılmıyor.
  shortLabel: 'Analiz',
  hint: 'Geçici bir poligon çizin; altında kalan envanterler sayılacak. Bu poligon kaydedilmez.',
  icon: (
    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M11 3 L19 8 L16 18 L6 18 L3 8 Z" strokeDasharray="3 2" />
      <circle cx="11" cy="11" r="3.5" />
      <path d="M13.6 13.6 L18 18" />
    </svg>
  ),
}

export default function DrawToolbar({ activeTool, onSelect, counts, disabled }) {
  const active =
    [...TOOLS, ANALYSIS_TOOL].find((tool) => tool.type === activeTool) ?? null
  const hasFeatures = counts.points + counts.lines + counts.polygons > 0

  function renderButton(tool, extraClass = '') {
    const isActive = tool.type === activeTool

    return (
      <button
        key={tool.type}
        type="button"
        className={`draw-tool ${extraClass} ${isActive ? 'draw-tool--active' : ''}`.trim()}
        // aria-pressed ekran okuyucuya hangi aracın seçili olduğunu söyler.
        aria-pressed={isActive}
        // Etiket dar ekranda gizlenebiliyor; ekran okuyucu yine tam adı okusun.
        aria-label={tool.label}
        disabled={disabled}
        // Aynı butona tekrar basmak aracı kapatsın.
        onClick={() => onSelect(isActive ? null : tool.type)}
      >
        {tool.icon}
        <span className="draw-tool__label">{tool.label}</span>
        {tool.shortLabel && <span className="draw-tool__label-short">{tool.shortLabel}</span>}
        {tool.countKey && <span className="draw-tool__count">{counts[tool.countKey]}</span>}
      </button>
    )
  }

  return (
    <div className="draw-toolbar">
      <div className="draw-toolbar__buttons" role="group" aria-label="Harita araçları">
        {TOOLS.map((tool) => renderButton(tool))}

        <span className="draw-toolbar__divider" aria-hidden="true" />

        {renderButton(ANALYSIS_TOOL, 'draw-tool--analysis')}
      </div>

      {active ? (
        <p className="draw-toolbar__hint">
          {active.hint} İptal için <kbd>Esc</kbd>.
        </p>
      ) : (
        // Silme özelliği tıklamayla çalışıyor; ipucu olmadan kimse bulamaz.
        hasFeatures && (
          <p className="draw-toolbar__hint draw-toolbar__hint--muted">
            Silmek için haritadaki bir çizime tıklayın.
          </p>
        )
      )}
    </div>
  )
}
