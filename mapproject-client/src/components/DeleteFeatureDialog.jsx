import { useState } from 'react'
import './FeatureDialog.css'

const TYPE_LABELS = {
  Point: 'nokta',
  LineString: 'çizgi',
  Polygon: 'poligon',
}

export default function DeleteFeatureDialog({ name, geometryType, onDelete, onCancel }) {
  const [isDeleting, setIsDeleting] = useState(false)
  const [error, setError] = useState('')

  async function handleDelete() {
    setError('')
    setIsDeleting(true)

    try {
      await onDelete()
    } catch (err) {
      if (err.response?.status === 404) {
        setError('Bu kayıt veritabanında bulunamadı.')
      } else if (err.response?.status !== 401) {
        setError('Silinemedi. Sunucuya ulaşılamıyor olabilir.')
      }
      setIsDeleting(false)
    }
  }

  return (
    <div className="feature-dialog__backdrop">
      <div className="feature-dialog" role="dialog" aria-modal="true">
        <h2 className="feature-dialog__title">Çizimi sil</h2>
        <p className="feature-dialog__name">{name}</p>
        <p className="feature-dialog__subtitle">
          Bu {TYPE_LABELS[geometryType]} haritadan ve listelerden kaldırılacak.
          Kayıt veritabanında saklanmaya devam eder (soft delete).
        </p>

        {error && (
          <p className="feature-dialog__error" role="alert">
            {error}
          </p>
        )}

        <div className="feature-dialog__actions">
          <button
            type="button"
            className="feature-dialog__cancel"
            onClick={onCancel}
            disabled={isDeleting}
          >
            Vazgeç
          </button>
          <button
            type="button"
            className="feature-dialog__danger"
            onClick={handleDelete}
            disabled={isDeleting}
          >
            {isDeleting ? 'Siliniyor…' : 'Sil'}
          </button>
        </div>
      </div>
    </div>
  )
}
