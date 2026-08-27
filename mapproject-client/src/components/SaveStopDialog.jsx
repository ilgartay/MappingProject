import { useState } from 'react'
import './FeatureDialog.css'

/**
 * Yeni durağın bilgilerini soran pencere.
 *
 * Güzergah açılır kutuda seçiliyor. Panelden bir hat seçilmişse o hat
 * hazır geliyor - operatör hattı seçip haritaya tıklıyor, aynı soruyu
 * her durakta yeniden cevaplamak zorunda kalmıyor.
 */
export default function SaveStopDialog({ routes, defaultRouteId, onSave, onCancel }) {
  const [name, setName] = useState('')
  const [routeId, setRouteId] = useState(defaultRouteId ? String(defaultRouteId) : '')
  const [error, setError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  const selectable = routes.filter((r) => r.isActive)

  async function handleSubmit(event) {
    event.preventDefault()

    if (!name.trim()) {
      setError('Lütfen bir durak adı girin.')
      return
    }

    if (!routeId) {
      setError('Lütfen bir güzergah seçin.')
      return
    }

    setError('')
    setIsSaving(true)

    try {
      await onSave({ name: name.trim(), routeId: Number(routeId) })
    } catch (err) {
      setError(err.response?.data?.message ?? 'Durak kaydedilemedi.')
      setIsSaving(false)
    }
  }

  return (
    <div className="feature-dialog__backdrop">
      <form className="feature-dialog" onSubmit={handleSubmit}>
        <h2>Durak ekle</h2>
        <p className="feature-dialog__subtitle">Durak adını girin ve güzergahını seçin.</p>

        <label className="feature-dialog__label" htmlFor="stop-name">
          Durak adı
        </label>
        <input
          id="stop-name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Örn. Kızılay"
          maxLength={100}
          autoFocus
        />

        <label className="feature-dialog__label feature-dialog__label--spaced" htmlFor="stop-route">
          Güzergah
        </label>
        <select id="stop-route" value={routeId} onChange={(e) => setRouteId(e.target.value)}>
          <option value="">Seçiniz…</option>
          {selectable.map((route) => (
            <option key={route.id} value={route.id}>
              {route.name}
            </option>
          ))}
        </select>

        {selectable.length === 0 && (
          <p className="feature-dialog__error" role="alert">
            Tanımlı güzergah yok. Önce Güzergah Yönetimi panelinden bir güzergah ekleyin.
          </p>
        )}

        {error && (
          <p className="feature-dialog__error" role="alert">
            {error}
          </p>
        )}

        <div className="feature-dialog__actions">
          <button type="button" className="feature-dialog__cancel" onClick={onCancel}>
            İptal
          </button>
          <button type="submit" className="feature-dialog__submit" disabled={isSaving}>
            {isSaving ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </div>
      </form>
    </div>
  )
}
