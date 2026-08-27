import { useEffect, useRef, useState } from 'react'
import { matchesQuery } from '../pages/admin/adminFilter'
import './PoiSearch.css'

/** Aynı anda gösterilecek en fazla sonuç; liste haritayı kapatmasın. */
const MAX_RESULTS = 8

/**
 * POI arama barı.
 *
 * Arama tarayıcıda yapılıyor: POI'lerin tamamı harita açılırken zaten tek
 * istekte geliyor, her tuş vuruşunda sunucuya gitmenin anlamı yok. Kayıt
 * sayısı binlere çıkarsa burası sunucu tarafı aramaya döner.
 *
 * Yetki istemiyor - Kullanıcı rolü de arayabilsin diye. Zaten POI listesi
 * herkese açık, arama da onun üstünde çalışıyor.
 */
export default function PoiSearch({ pois, onSelect }) {
  const [query, setQuery] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef(null)

  // Dışarı tıklanınca liste kapansın; harita ile etkileşimde açık kalmasın.
  useEffect(() => {
    function onPointerDown(event) {
      if (!containerRef.current?.contains(event.target)) {
        setIsOpen(false)
      }
    }

    document.addEventListener('pointerdown', onPointerDown)
    return () => document.removeEventListener('pointerdown', onPointerDown)
  }, [])

  // Ad, kategori ve mesai saatleri birlikte taranıyor: "eczane" yazan
  // kategoriden, "7/24" yazan mesai saatinden de bulabilsin.
  const results = query.trim()
    ? pois
        .filter((poi) => matchesQuery(query, poi.name, poi.categoryPath, poi.workingHours))
        .slice(0, MAX_RESULTS)
    : []

  function handleSelect(poi) {
    setQuery(poi.name)
    setIsOpen(false)
    onSelect(poi)
  }

  function handleKeyDown(event) {
    if (event.key === 'Escape') {
      setIsOpen(false)
      return
    }

    // Enter tek sonuç varsa doğrudan ona gitsin; liste açıkken en
    // olası sonucu seçmek için fareye uzanmak gerekmesin.
    if (event.key === 'Enter' && results.length > 0) {
      event.preventDefault()
      handleSelect(results[0])
    }
  }

  return (
    <div className="poi-search" ref={containerRef}>
      <div className="poi-search__box">
        <span className="poi-search__icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" width="17" height="17" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            <circle cx="11" cy="11" r="7" />
            <path d="M16.5 16.5 L21 21" />
          </svg>
        </span>

        <input
          type="search"
          className="poi-search__input"
          value={query}
          placeholder="İlgi noktası ara"
          aria-label="İlgi noktası ara"
          onChange={(e) => {
            setQuery(e.target.value)
            setIsOpen(true)
          }}
          onFocus={() => setIsOpen(true)}
          onKeyDown={handleKeyDown}
        />
      </div>

      {isOpen && query.trim() && (
        <ul className="poi-search__results">
          {results.length === 0 ? (
            <li className="poi-search__empty">Sonuç yok</li>
          ) : (
            results.map((poi) => (
              <li key={poi.id}>
                <button type="button" className="poi-search__result" onClick={() => handleSelect(poi)}>
                  <span className="poi-search__name">{poi.name}</span>
                  <span className="poi-search__meta">
                    {poi.categoryPath || poi.categoryName}
                    {poi.workingHours && ` · ${poi.workingHours}`}
                  </span>
                </button>
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  )
}
