import { useAuth } from '../auth/useAuth'
import './DrawToolbar.css'

// macOS'ta geri alma kısayolu Cmd, diğer sistemlerde Ctrl. Kod ikisini de
// kabul ediyor; ipucunda kullanıcının gerçekten bastığı tuşu yazıyoruz.
const UNDO_KEY = navigator.userAgent.includes('Mac') ? '⌘' : 'Ctrl'

const TOOLS = [
  {
    type: 'Point',
    label: 'Nokta',
    permission: 'point.create',
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
    permission: 'line.create',
    countKey: 'lines',
    hint: 'Her tıklama bir kırılma noktası ekler, çift tıklayarak bitirin.',
    // Birden fazla noktadan oluşuyor, yani geri alınacak bir "son nokta" var.
    canUndo: true,
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
    permission: 'polygon.create',
    countKey: 'polygons',
    hint: 'Köşeleri tıklayın, çift tıklayarak alanı kapatın.',
    canUndo: true,
    icon: (
      <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M12 3 L21 10 L17 20 L7 20 L3 10 Z" />
      </svg>
    ),
  },
  // POI geometri olarak nokta ama ayrı bir kavram: kullanıcının kendi
  // çizimi değil, herkesin gördüğü ortak veri. Sayacı da yok - kaç POI
  // olduğu kullanıcıya özel bir bilgi değil.
  {
    type: 'Poi',
    label: 'POI',
    permission: 'poi.create',
    hint: 'Haritaya tıklayın; ardından isim, kategori ve mesai saatlerini girin.',
    icon: (
      <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <path d="M12 21s7-6.2 7-11a7 7 0 1 0-14 0c0 4.8 7 11 7 11Z" />
        <circle cx="12" cy="10" r="2.5" fill="currentColor" stroke="none" />
      </svg>
    ),
  },
]

// Analiz aracı çizim araçlarından ayrı duruyor: geometri kaydetmiyor,
// sadece geçici bir poligonla sorgu yapıyor.
const ANALYSIS_TOOL = {
  type: 'Analysis',
  label: 'Envanter Analizi',
  permission: 'analysis.run',
  // Dar ekranda tam etiket sığmıyor; ikon tek başına da anlaşılmıyor.
  shortLabel: 'Analiz',
  hint: 'Geçici bir poligon çizin; altında kalan envanterler sayılacak. Bu poligon kaydedilmez.',
  canUndo: true,
  icon: (
    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M11 3 L19 8 L16 18 L6 18 L3 8 Z" strokeDasharray="3 2" />
      <circle cx="11" cy="11" r="3.5" />
      <path d="M13.6 13.6 L18 18" />
    </svg>
  ),
}

// Isı haritası bir çizim aracı değil, açık/kapalı bir gösterim seçeneği:
// kullanıcı bir şey çizmiyor, var olan noktaların yoğunluğuna bakıyor.
// Bu yüzden activeTool'a girmiyor, kendi durumunu taşıyor.
const HEATMAP_TOOL = {
  label: 'Isı Haritası Analizi',
  permission: 'analysis.heatmap',
  shortLabel: 'Isı',
  icon: (
    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="2.5" fill="currentColor" stroke="none" />
      <circle cx="12" cy="12" r="6" opacity="0.75" />
      <circle cx="12" cy="12" r="9.5" opacity="0.4" />
    </svg>
  ),
}

export default function DrawToolbar({
  activeTool,
  onSelect,
  counts,
  disabled,
  canDelete,
  isHeatmapOn,
  onToggleHeatmap,
}) {
  const { hasPermission } = useAuth()

  // Yetkisi olmayan araç hiç görünmüyor. Asıl kontrol sunucuda ama
  // kullanıcıya çalışmayacak bir düğme göstermek de doğru değil.
  const visibleTools = TOOLS.filter((tool) => hasPermission(tool.permission))
  const canAnalyze = hasPermission(ANALYSIS_TOOL.permission)
  const canHeatmap = hasPermission(HEATMAP_TOOL.permission)

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
        {visibleTools.map((tool) => renderButton(tool))}

        {visibleTools.length > 0 && canAnalyze && (
          <span className="draw-toolbar__divider" aria-hidden="true" />
        )}

        {canAnalyze && renderButton(ANALYSIS_TOOL, 'draw-tool--analysis')}

        {canHeatmap && (
          <button
            type="button"
            className={`draw-tool draw-tool--analysis ${isHeatmapOn ? 'draw-tool--active' : ''}`.trim()}
            aria-pressed={isHeatmapOn}
            aria-label={HEATMAP_TOOL.label}
            onClick={onToggleHeatmap}
          >
            {HEATMAP_TOOL.icon}
            <span className="draw-tool__label">{HEATMAP_TOOL.label}</span>
            <span className="draw-tool__label-short">{HEATMAP_TOOL.shortLabel}</span>
          </button>
        )}

        {visibleTools.length === 0 && !canAnalyze && !canHeatmap && (
          <span className="draw-toolbar__readonly">Görüntüleme yetkisi</span>
        )}
      </div>

      {active ? (
        <p className="draw-toolbar__hint">
          {active.hint}{' '}
          {active.canUndo ? (
            <>
              Son noktayı geri almak için <kbd>{UNDO_KEY}</kbd>+<kbd>Z</kbd>, iptal için{' '}
              <kbd>Esc</kbd>.
            </>
          ) : (
            <>
              İptal için <kbd>Esc</kbd>.
            </>
          )}
        </p>
      ) : (
        // Silme özelliği tıklamayla çalışıyor; ipucu olmadan kimse bulamaz.
        hasFeatures && (
          <p className="draw-toolbar__hint draw-toolbar__hint--muted">
            {canDelete
              ? 'Düzenlemek veya silmek için bir çizime tıklayın.'
              : 'Detayını görmek için bir çizime tıklayın.'}
          </p>
        )
      )}
    </div>
  )
}
