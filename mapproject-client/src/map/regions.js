/**
 * Türkiye'nin coğrafi bölgeleri - alan yetkisi verirken hazır seçenek.
 *
 * DİKKAT: Bu sınırlar YAKLAŞIKTIR, resmi il/bölge sınırları değil.
 * Amaç yöneticiye "Ege'de çalışsın" demenin pratik bir yolunu vermek;
 * gerçek sınır verisi gerekirse il sınırlarından türetilmiş bir veri
 * kümesi (ör. TÜİK/OSM sınırları) alınıp poi_category gibi bir tabloya
 * yüklenmeli, buradaki elle yazılmış köşeler onunla değiştirilmeli.
 *
 * Koordinatlar [boylam, enlem] sırasında ve EPSG:4326.
 * Bölgeler ortak köşe noktaları paylaşacak şekilde yazıldı; böylece
 * yan yana seçildiklerinde aralarında boşluk kalmıyor.
 */
export const TURKEY_REGIONS = [
  {
    id: 'marmara',
    name: 'Marmara',
    coordinates: [
      [25.9, 40.2], [26.0, 42.1], [31.4, 42.1], [31.4, 40.9],
      [30.0, 39.8], [27.2, 39.6], [26.3, 39.9],
    ],
  },
  {
    id: 'ege',
    name: 'Ege',
    coordinates: [
      [25.9, 36.9], [25.9, 40.2], [26.3, 39.9], [27.2, 39.6],
      [30.0, 39.8], [30.3, 38.2], [29.2, 36.8],
    ],
  },
  {
    id: 'akdeniz',
    name: 'Akdeniz',
    coordinates: [
      [29.2, 36.8], [30.3, 38.2], [32.6, 38.0], [36.3, 37.5],
      [36.6, 36.2], [33.5, 36.0], [30.5, 36.2],
    ],
  },
  {
    id: 'ic-anadolu',
    name: 'İç Anadolu',
    coordinates: [
      [30.3, 38.2], [30.0, 39.8], [31.4, 40.9], [35.6, 40.5],
      [37.2, 39.6], [36.3, 37.5], [32.6, 38.0],
    ],
  },
  {
    id: 'karadeniz',
    name: 'Karadeniz',
    coordinates: [
      [31.4, 40.9], [31.4, 42.1], [41.9, 41.4], [41.6, 40.6],
      [39.6, 40.2], [35.6, 40.5],
    ],
  },
  {
    id: 'dogu-anadolu',
    name: 'Doğu Anadolu',
    coordinates: [
      [37.2, 39.6], [39.6, 40.2], [41.6, 40.6], [44.8, 39.7],
      [44.4, 37.4], [42.3, 37.2], [38.6, 37.7],
    ],
  },
  {
    id: 'guneydogu-anadolu',
    name: 'Güneydoğu Anadolu',
    coordinates: [
      [36.3, 37.5], [38.6, 37.7], [42.3, 37.2], [42.2, 36.6],
      [38.0, 36.5], [36.6, 36.2],
    ],
  },
]

/** "30.3 38.2, 30 39.8, ..." - WKT'nin halka gösterimi. */
function ring(coordinates) {
  // WKT halkası kapalı olmalı: son nokta ilkiyle aynı.
  const closed = [...coordinates, coordinates[0]]
  return closed.map(([lon, lat]) => `${lon} ${lat}`).join(', ')
}

/**
 * Seçilen bölgelerden WKT üretir.
 *
 * Tek bölge POLYGON, birden çoğu MULTIPOLYGON döner. Neden birleştirme
 * (union) yapmıyoruz: bölgeler birbirinin üstüne binmiyor, sadece yan
 * yana duruyorlar. MULTIPOLYGON tam olarak bunu ifade ediyor ve
 * tarayıcıya geometri kütüphanesi eklemek gerekmiyor.
 *
 * @param {string[]} ids seçili bölge kimlikleri
 * @returns {string|null} boş seçimde null
 */
export function regionsToWkt(ids) {
  const selected = TURKEY_REGIONS.filter((region) => ids.includes(region.id))

  if (selected.length === 0) return null

  if (selected.length === 1) {
    return `POLYGON ((${ring(selected[0].coordinates)}))`
  }

  const parts = selected.map((region) => `((${ring(region.coordinates)}))`)
  return `MULTIPOLYGON (${parts.join(', ')})`
}
