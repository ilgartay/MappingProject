import { HEATMAP_GRADIENT } from '../map/heatmapScale'
import './HeatmapLegend.css'

/**
 * Isı haritası lejantı.
 *
 * Yoğunluk her zaman 0-1 arasına ölçeklenir: 1, o görünümdeki en yoğun
 * bölge demek, mutlak bir nokta sayısı değil. Bu ayrımı yazıyla da
 * söylüyoruz, yoksa "1 = bir nokta" diye okunabilir.
 */
export default function HeatmapLegend() {
  return (
    <div className="heatmap-legend" role="figure" aria-label="Isı haritası yoğunluk ölçeği">
      <p className="heatmap-legend__title">Nokta yoğunluğu</p>

      <div className="heatmap-legend__bar" style={{ background: HEATMAP_GRADIENT }} />

      <div className="heatmap-legend__ticks" aria-hidden="true">
        <span>0.0</span>
        <span>0.2</span>
        <span>0.4</span>
        <span>0.6</span>
        <span>0.8</span>
        <span>1.0</span>
      </div>

      <p className="heatmap-legend__note">
        0 = seyrek, 1 = görünümdeki en yoğun bölge
      </p>
    </div>
  )
}
