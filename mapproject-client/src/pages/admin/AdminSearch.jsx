/**
 * Admin listelerinin üstündeki arama kutusu.
 *
 * Filtreleme tarayıcıda yapılıyor: kullanıcı ve rol listeleri zaten tek
 * istekte tamamıyla geliyor, her tuş vuruşunda sunucuya gitmenin anlamı yok.
 * Liste binlerce satıra çıkarsa burası sunucu tarafı aramaya döner.
 *
 * @param {object} props
 * @param {string} props.value arama metni
 * @param {(value: string) => void} props.onChange
 * @param {string} props.label placeholder ve ekran okuyucu etiketi
 * @param {number} props.shown filtreden geçen satır sayısı
 * @param {number} props.total toplam satır sayısı
 */
export default function AdminSearch({ value, onChange, label, shown, total }) {
  return (
    <div className="admin-search">
      <span className="admin-search__icon" aria-hidden="true">
        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
          <circle cx="11" cy="11" r="7" />
          <path d="M16.5 16.5 L21 21" />
        </svg>
      </span>

      <input
        type="search"
        className="admin-search__input"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={label}
        aria-label={label}
      />

      {/* Sayaç yalnızca arama varken görünüyor; boşken "12 / 12" yazmak
          bilgi vermeden yer kaplardı. */}
      {value && (
        <span className="admin-search__count" role="status">
          {shown} / {total}
        </span>
      )}
    </div>
  )
}
