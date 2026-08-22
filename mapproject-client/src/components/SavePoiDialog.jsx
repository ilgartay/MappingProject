import { useState } from 'react'
import { categoryLabel } from '../api/poi'
import './FeatureDialog.css'

/**
 * Yeni POI'nin bilgilerini soran pencere.
 *
 * Kategoriler admin'in tanımladığı ağaçtan geliyor; açılır kutuda
 * "Yeme-İçme → Restoran" biçiminde gösteriliyor ki aynı adı taşıyan
 * iki alt kategori karışmasın.
 */
export default function SavePoiDialog({ categories, onSave, onCancel }) {
  const [name, setName] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [workingHours, setWorkingHours] = useState('')
  const [error, setError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  // Pasif kategoriler seçilemez; sunucu da reddediyor.
  const selectable = categories.filter((c) => c.isActive)

  async function handleSubmit(event) {
    event.preventDefault()

    if (!name.trim()) {
      setError('Lütfen bir ad girin.')
      return
    }

    if (!categoryId) {
      setError('Lütfen bir kategori seçin.')
      return
    }

    setError('')
    setIsSaving(true)

    try {
      await onSave({
        name: name.trim(),
        categoryId: Number(categoryId),
        workingHours: workingHours.trim(),
      })
    } catch (err) {
      if (err.response?.data?.message) {
        setError(err.response.data.message)
      } else {
        setError('Kaydedilemedi. Sunucuya ulaşılamıyor olabilir.')
      }
      setIsSaving(false)
    }
  }

  return (
    <div className="feature-dialog__backdrop">
      <form className="feature-dialog" onSubmit={handleSubmit}>
        <h2>POI kaydet</h2>
        <p className="feature-dialog__subtitle">İlgi noktasının bilgilerini girin.</p>

        <label className="feature-dialog__label" htmlFor="poi-name">
          İsim
        </label>
        <input
          id="poi-name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Örn. Kızılay Eczanesi"
          maxLength={150}
          autoFocus
        />

        <label className="feature-dialog__label feature-dialog__label--spaced" htmlFor="poi-category">
          Kategori
        </label>
        <select
          id="poi-category"
          value={categoryId}
          onChange={(e) => setCategoryId(e.target.value)}
        >
          <option value="">Seçiniz…</option>
          {selectable.map((category) => (
            <option key={category.id} value={category.id}>
              {categoryLabel(category)}
            </option>
          ))}
        </select>

        <label className="feature-dialog__label feature-dialog__label--spaced" htmlFor="poi-hours">
          Mesai saatleri
        </label>
        <input
          id="poi-hours"
          value={workingHours}
          onChange={(e) => setWorkingHours(e.target.value)}
          placeholder="Örn. 09:00 - 18:00 veya 7/24"
          maxLength={100}
        />

        {selectable.length === 0 && (
          <p className="feature-dialog__error" role="alert">
            Tanımlı kategori yok. Yöneticinin admin panelinden kategori eklemesi gerekiyor.
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
          <button type="submit" className="feature-dialog__save" disabled={isSaving}>
            {isSaving ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </div>
      </form>
    </div>
  )
}
