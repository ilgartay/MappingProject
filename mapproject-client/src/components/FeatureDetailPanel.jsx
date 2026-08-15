import { useEffect, useRef, useState } from 'react'
import { PRESET_COLORS } from '../map/colors'
import './FeatureDetailPanel.css'

const TYPE_LABELS = {
  Point: 'Nokta',
  LineString: 'Çizgi',
  Polygon: 'Poligon',
}

const GEOMETRY_HINTS = {
  Point: 'Noktayı sürükleyerek taşıyabilirsiniz.',
  LineString: 'Kırılma noktalarını sürükleyin; kenara tıklayıp yeni nokta ekleyebilirsiniz.',
  Polygon: 'Köşeleri sürükleyin; kenara tıklayıp yeni köşe ekleyebilirsiniz.',
}

/**
 * Haritadaki bir objeye tıklanınca açılan detay penceresi.
 * İsim ve rengi buradan, geometriyi harita üzerinde sürükleyerek düzenler;
 * "Kaydet" üçünü birlikte gönderir.
 */
export default function FeatureDetailPanel({
  feature,
  onSave,
  onCancel,
  onDeleteRequest,
}) {
  const [name, setName] = useState(feature.name ?? '')
  const [color, setColor] = useState(feature.color ?? PRESET_COLORS[0])
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState('')
  const inputRef = useRef(null)

  useEffect(() => {
    inputRef.current?.focus()
  }, [])

  async function handleSubmit(event) {
    event.preventDefault()

    const trimmed = name.trim()
    if (!trimmed) {
      setError('Lütfen bir ad girin.')
      return
    }

    setError('')
    setIsSaving(true)

    try {
      await onSave(trimmed, color)
    } catch (err) {
      if (err.response?.status === 404) {
        setError('Kayıt bulunamadı, silinmiş olabilir.')
      } else if (err.response?.status === 400) {
        setError(err.response.data?.message ?? 'Güncellenemedi.')
      } else if (err.response?.status !== 401) {
        setError('Güncellenemedi. Sunucuya ulaşılamıyor olabilir.')
      }
      setIsSaving(false)
    }
  }

  return (
    <form className="detail-panel" onSubmit={handleSubmit}>
      <header className="detail-panel__header">
        <h2 className="detail-panel__title">
          {TYPE_LABELS[feature.geometryType]} detayı
        </h2>
        <span className="detail-panel__id">#{feature.id}</span>
      </header>

      <label className="detail-panel__label" htmlFor="detail-name">
        İsim
      </label>
      <input
        id="detail-name"
        ref={inputRef}
        value={name}
        onChange={(e) => setName(e.target.value)}
        maxLength={100}
      />

      <span className="detail-panel__label detail-panel__label--spaced">Renk</span>
      <div className="detail-panel__colors">
        {PRESET_COLORS.map((preset) => (
          <button
            key={preset}
            type="button"
            className={
              preset === color
                ? 'detail-panel__swatch detail-panel__swatch--active'
                : 'detail-panel__swatch'
            }
            style={{ background: preset }}
            aria-label={`Renk ${preset}`}
            aria-pressed={preset === color}
            onClick={() => setColor(preset)}
          />
        ))}
        <label className="detail-panel__picker">
          <input
            type="color"
            value={color}
            onChange={(e) => setColor(e.target.value)}
            aria-label="Özel renk seç"
          />
        </label>
      </div>

      {/* Geometri düzenleme harita üzerinde yapılıyor; pencere bunu haber veriyor. */}
      <p className="detail-panel__hint">
        <strong>Konum:</strong> {GEOMETRY_HINTS[feature.geometryType]}
      </p>

      {error && (
        <p className="detail-panel__error" role="alert">
          {error}
        </p>
      )}

      <div className="detail-panel__actions">
        <button
          type="button"
          className="detail-panel__delete"
          onClick={onDeleteRequest}
          disabled={isSaving}
        >
          Sil
        </button>

        <div className="detail-panel__actions-right">
          <button
            type="button"
            className="detail-panel__cancel"
            onClick={onCancel}
            disabled={isSaving}
          >
            Vazgeç
          </button>
          <button type="submit" className="detail-panel__submit" disabled={isSaving}>
            {isSaving ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </div>
      </div>
    </form>
  )
}
