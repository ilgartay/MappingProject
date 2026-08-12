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

export default function DrawToolbar({ activeTool, onSelect, counts, disabled }) {
  const active = TOOLS.find((tool) => tool.type === activeTool)

  return (
    <div className="draw-toolbar">
      <div className="draw-toolbar__buttons" role="group" aria-label="Çizim araçları">
        {TOOLS.map((tool) => {
          const isActive = tool.type === activeTool

          return (
            <button
              key={tool.type}
              type="button"
              className={isActive ? 'draw-tool draw-tool--active' : 'draw-tool'}
              // aria-pressed ekran okuyucuya hangi aracın seçili olduğunu söyler.
              aria-pressed={isActive}
              disabled={disabled}
              // Aynı butona tekrar basmak aracı kapatsın.
              onClick={() => onSelect(isActive ? null : tool.type)}
            >
              {tool.icon}
              <span>{tool.label}</span>
              <span className="draw-tool__count">{counts[tool.countKey]}</span>
            </button>
          )
        })}
      </div>

      {active && (
        <p className="draw-toolbar__hint">
          {active.hint} İptal için <kbd>Esc</kbd>.
        </p>
      )}
    </div>
  )
}
