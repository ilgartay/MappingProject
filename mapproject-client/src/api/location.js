import client from './client'

/** İl listesi (sınır geometrisi olmadan) - analiz panelindeki açılır kutu. */
export async function fetchProvinces() {
  const { data } = await client.get('/api/Province')
  return data
}

/** Tek il, sınırıyla birlikte; harita seçileni çizsin diye. */
export async function fetchProvince(id) {
  const { data } = await client.get(`/api/Province/${id}`)
  return data
}

/** Kurallar sunucuda da uygulanıyor; buradakiler arayüzü canlı tutmak için. */
export const CRITERIA_RULES = { min: 2, max: 5, total: 100 }

/**
 * Kriterlerin sunucuya gidecek biçimi: "4:70,5:30".
 * Kategorisi seçilmemiş satırlar atlanıyor.
 */
export function criteriaToParam(criteria) {
  return criteria
    .filter((c) => c.categoryId)
    .map((c) => `${c.categoryId}:${c.weight}`)
    .join(',')
}

/**
 * Analizin başlatılabilir olup olmadığı ve olmadıysa sebebi.
 * Sebebi de döndürüyoruz ki kullanıcı düğmenin neden kapalı olduğunu
 * tahmin etmek zorunda kalmasın.
 */
export function validateAnalysis({ criteria, provinceId, areaWkt }) {
  const filled = criteria.filter((c) => c.categoryId)

  if (!provinceId && !areaWkt) {
    return 'Hedef bölge seçin: listeden bir il ya da haritada bir alan.'
  }

  if (filled.length < CRITERIA_RULES.min) {
    return `En az ${CRITERIA_RULES.min} kriter seçin.`
  }

  if (new Set(filled.map((c) => c.categoryId)).size !== filled.length) {
    return 'Aynı kategori birden fazla kez seçilemez.'
  }

  const total = filled.reduce((sum, c) => sum + (Number(c.weight) || 0), 0)

  if (total !== CRITERIA_RULES.total) {
    return `Puanların toplamı ${CRITERIA_RULES.total} olmalı. Şu an: ${total}.`
  }

  return null
}
