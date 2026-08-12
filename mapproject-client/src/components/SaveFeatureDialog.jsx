import { useEffect, useRef, useState } from 'react'
import './SaveFeatureDialog.css'

const TYPE_LABELS = {
  Point: 'Nokta',
  LineString: 'Çizgi',
  Polygon: 'Poligon',
}

export default function SaveFeatureDialog({ geometryType, onSave, onCancel }) {
  const [name, setName] = useState('')
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState('')
  const inputRef = useRef(null)

  // Pencere açılır açılmaz imleç kutuda olsun - kullanıcı fareye uzanmasın.
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
      await onSave(trimmed)
    } catch (err) {
      if (err.response?.status === 400) {
        setError(err.response.data?.message ?? 'Geometri kaydedilemedi.')
      } else if (err.response?.status !== 401) {
        setError('Kaydedilemedi. Sunucuya ulaşılamıyor olabilir.')
      }
      setIsSaving(false)
    }
  }

  return (
    <div className="save-dialog__backdrop">
      <form className="save-dialog" onSubmit={handleSubmit}>
        <h2 className="save-dialog__title">
          {TYPE_LABELS[geometryType]} kaydet
        </h2>
        <p className="save-dialog__subtitle">
          Çizimin veritabanında saklanacağı adı girin.
        </p>

        <label className="save-dialog__label" htmlFor="feature-name">
          Ad
        </label>
        <input
          id="feature-name"
          ref={inputRef}
          value={name}
          onChange={(e) => setName(e.target.value)}
          maxLength={100}
          placeholder="Örn. Ankara merkez"
        />

        {error && (
          <p className="save-dialog__error" role="alert">
            {error}
          </p>
        )}

        <div className="save-dialog__actions">
          <button type="button" className="save-dialog__cancel" onClick={onCancel} disabled={isSaving}>
            İptal
          </button>
          <button type="submit" className="save-dialog__submit" disabled={isSaving}>
            {isSaving ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </div>
      </form>
    </div>
  )
}
