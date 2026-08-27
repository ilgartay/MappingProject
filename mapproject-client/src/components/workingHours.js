/**
 * Mesai saatlerinin veri tarafı: gün listesi, saat seçenekleri ve
 * seçimleri veritabanına yazılan metne çeviren biçimleyici.
 *
 * Neden WorkingHoursPicker.jsx'ten ayrı bir dosya: Vite'ın hızlı
 * yenileme (fast refresh) özelliği, bir dosya hem bileşen hem başka
 * şeyler dışa aktardığında çalışmıyor - ESLint de bunu hata sayıyor.
 */

// key: haftanın kaçıncı günü. Ardışıklık kontrolü buna bakıyor,
// o yüzden pazartesi 1'den başlayıp sırayla gidiyor.
export const DAYS = [
  { key: 1, short: 'Pzt' },
  { key: 2, short: 'Sal' },
  { key: 3, short: 'Çar' },
  { key: 4, short: 'Per' },
  { key: 5, short: 'Cum' },
  { key: 6, short: 'Cmt' },
  { key: 7, short: 'Paz' },
]

/** 00:00'dan 23:30'a yarım saatlik adımlar. */
export const TIMES = Array.from({ length: 48 }, (_, i) => {
  const hour = String(Math.floor(i / 2)).padStart(2, '0')
  return `${hour}:${i % 2 ? '30' : '00'}`
})

/** Form ilk açıldığında dolu gelen değer: en sık kullanılan mesai. */
export const DEFAULT_HOURS = {
  allDay: false,
  days: [1, 2, 3, 4, 5],
  opensAt: '09:00',
  closesAt: '18:00',
}

/**
 * Seçimleri veritabanına yazılacak tek satırlık metne çevirir.
 * Örn. { days: [1,2,3,4,5] } -> "Pzt-Cum 09:00-18:00".
 */
export function formatWorkingHours({ allDay, days, opensAt, closesAt }) {
  if (allDay) return '7/24'

  const selected = DAYS.filter((day) => days.includes(day.key))
  if (selected.length === 0) return ''

  const label = selected.length === DAYS.length ? 'Her gün' : joinDays(selected)
  return `${label} ${opensAt}-${closesAt}`
}

/**
 * Ardışık günleri aralığa toplar: Pzt+Sal+Çar+Per+Cum -> "Pzt-Cum".
 * İki günlük dizi kısaltılmıyor; "Pzt-Sal" yazmak "Pzt, Sal"dan daha
 * kısa değil ama okurken aralık sanılıyor.
 */
function joinDays(selected) {
  const parts = []
  let runStart = 0

  for (let i = 0; i < selected.length; i += 1) {
    const isLast = i === selected.length - 1
    const runEnds = isLast || selected[i + 1].key !== selected[i].key + 1
    if (!runEnds) continue

    if (i - runStart + 1 >= 3) {
      parts.push(`${selected[runStart].short}-${selected[i].short}`)
    } else {
      for (let j = runStart; j <= i; j += 1) parts.push(selected[j].short)
    }
    runStart = i + 1
  }

  return parts.join(', ')
}
