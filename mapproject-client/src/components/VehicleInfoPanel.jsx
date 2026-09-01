import './PoiInfoPanel.css'
import './VehicleInfoPanel.css'

/** 10600 -> "10,6 km" */
function formatDistance(metres) {
  return `${(metres / 1000).toLocaleString('tr-TR', { maximumFractionDigits: 1 })} km`
}

/**
 * Hareket eden araca tıklanınca açılan bilgi kutusu.
 *
 * Durak ve POI panelleriyle aynı biçimi paylaşıyor; farkı ilerleme
 * çubuğu. Gösterilen değer canlı: MapView bu kutuya güzergah id'sini
 * değil aracın o anki durumunu veriyor ve her SignalR yayınında
 * yeniden çiziliyor.
 */
export default function VehicleInfoPanel({ vehicle, onClose }) {
  const percent = Math.round(vehicle.progress)

  return (
    <aside className="poi-panel" role="dialog" aria-label="Araç bilgisi">
      <header className="poi-panel__header">
        <h2>{vehicle.routeName}</h2>
        <button type="button" className="poi-panel__close" onClick={onClose} aria-label="Kapat">
          ×
        </button>
      </header>

      <div className="vehicle-panel__progress">
        <div className="vehicle-panel__bar">
          <div
            className="vehicle-panel__fill"
            style={{ width: `${percent}%`, background: vehicle.routeColor }}
          />
        </div>
        <strong className="vehicle-panel__percent">%{percent}</strong>
      </div>

      <dl className="poi-panel__list">
        <dt>Tamamlanan</dt>
        <dd>
          {formatDistance(vehicle.travelledMetres)} / {formatDistance(vehicle.totalMetres)}
        </dd>

        <dt>Kalan</dt>
        <dd>{formatDistance(Math.max(0, vehicle.totalMetres - vehicle.travelledMetres))}</dd>

        <dt>Durum</dt>
        <dd>
          <span className="poi-panel__on">Yolda</span>
        </dd>
      </dl>
    </aside>
  )
}
