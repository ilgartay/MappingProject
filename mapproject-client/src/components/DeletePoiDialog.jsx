import { useState } from 'react'
import './FeatureDialog.css'

/**
 * POI silme onayı.
 *
 * Çizim silme penceresinden ayrı duruyor çünkü metni farklı: POI
 * paylaşılan veri, silindiğinde yalnızca ekleyeni değil herkesi
 * etkiliyor. Kullanıcının bunu bilerek onaylaması gerekiyor.
 */
export default function DeletePoiDialog({ name, onDelete, onCancel }) {
  const [isDeleting, setIsDeleting] = useState(false)
  const [error, setError] = useState('')

  async function handleDelete() {
    setError('')
    setIsDeleting(true)

    try {
      await onDelete()
    } catch (err) {
      if (err.response?.status === 404) {
        setError('Bu POI veritabanında bulunamadı.')
      } else if (err.response?.status === 403) {
        setError('POI silme yetkiniz yok.')
      } else if (err.response?.status !== 401) {
        setError('Silinemedi. Sunucuya ulaşılamıyor olabilir.')
      }
      setIsDeleting(false)
    }
  }

  return (
    <div className="feature-dialog__backdrop">
      <div className="feature-dialog" role="dialog" aria-modal="true">
        <h2 className="feature-dialog__title">POI'yi sil</h2>
        <p className="feature-dialog__name">{name}</p>
        <p className="feature-dialog__subtitle">
          Bu ilgi noktası haritadan ve listelerden kaldırılacak; POI'ler ortak
          veri olduğu için herkesin haritasından kalkar. Kayıt veritabanında
          saklanmaya devam eder (soft delete).
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
