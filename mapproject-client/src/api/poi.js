import client from './client'

// --- POI ---

/**
 * Tüm POI'ler. Çizimlerden farklı olarak kullanıcıya göre filtrelenmiyor:
 * bir eczanenin konumu herkes için aynı bilgi.
 */
export async function fetchPois() {
  const { data } = await client.get('/api/Poi')
  return data
}

export async function createPoi(payload) {
  const { data } = await client.post('/api/Poi', payload)
  return data
}

export async function updatePoi(id, payload) {
  const { data } = await client.put(`/api/Poi/${id}`, payload)
  return data
}

/** Soft delete. */
export async function deletePoi(id) {
  await client.delete(`/api/Poi/${id}`)
}

// --- Kategoriler ---

/** Düz liste; POI formundaki açılır kutu bunu kullanıyor. */
export async function fetchCategories() {
  const { data } = await client.get('/api/PoiCategory')
  return data
}

/** Ağaç yapısı; admin panelindeki kategori yönetimi bunu kullanıyor. */
export async function fetchCategoryTree() {
  const { data } = await client.get('/api/PoiCategory/tree')
  return data
}

export async function createCategory(payload) {
  const { data } = await client.post('/api/PoiCategory', payload)
  return data
}

export async function updateCategory(id, payload) {
  const { data } = await client.put(`/api/PoiCategory/${id}`, payload)
  return data
}

export async function deleteCategory(id) {
  await client.delete(`/api/PoiCategory/${id}`)
}

/**
 * "Yeme-İçme → Restoran" - açılır kutuda ve listelerde kategoriyi
 * bağlamıyla göstermek için. Sunucu POI'lerde bu yolu zaten üretiyor;
 * kategori listesinde yalnızca üst kategorinin adı geldiği için
 * birleştirmeyi burada yapıyoruz.
 */
export function categoryLabel(category) {
  return category.parentName ? `${category.parentName} → ${category.name}` : category.name
}
