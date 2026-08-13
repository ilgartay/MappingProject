import { useState } from 'react'
import './CoordinateSearch.css'

/**
 * Enlem/boylam girilip haritada o noktaya gidilmesini sağlar.
 * Girdi EPSG:4326 (derece); 3857'ye çevirme işi MapView'de yapılıyor.
 */
export default function CoordinateSearch({ onSearch }) {
  const [latitude, setLatitude] = useState('')
  const [longitude, setLongitude] = useState('')
  const [error, setError] = useState('')

  function handleSubmit(event) {
    event.preventDefault()

    // Virgüllü yazım Türkçe klavyede çok yaygın: "39,93" da kabul edilsin.
    const lat = Number(latitude.replace(',', '.'))
    const lon = Number(longitude.replace(',', '.'))

    if (!latitude.trim() || !longitude.trim() || Number.isNaN(lat) || Number.isNaN(lon)) {
      setError('Enlem ve boylam sayı olmalı.')
      return
    }

    if (lat < -90 || lat > 90) {
      setError('Enlem -90 ile 90 arasında olmalı.')
      return
    }

    if (lon < -180 || lon > 180) {
      setError('Boylam -180 ile 180 arasında olmalı.')
      return
    }

    setError('')
    onSearch(lon, lat)
  }

  return (
    <form className="coord-search" onSubmit={handleSubmit}>
      <div className="coord-search__row">
        <label className="coord-search__field">
          <span>Enlem</span>
          <input
            value={latitude}
            onChange={(e) => setLatitude(e.target.value)}
            placeholder="39.93"
            inputMode="decimal"
          />
        </label>

        <label className="coord-search__field">
          <span>Boylam</span>
          <input
            value={longitude}
            onChange={(e) => setLongitude(e.target.value)}
            placeholder="32.86"
            inputMode="decimal"
          />
        </label>

        <button type="submit" className="coord-search__submit">
          Git
        </button>
      </div>

      {error && (
        <p className="coord-search__error" role="alert">
          {error}
        </p>
      )}
    </form>
  )
}
