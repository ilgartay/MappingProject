import client from './client'

// --- Kullanıcılar ---

export async function fetchUsers() {
  const { data } = await client.get('/api/User')
  return data
}

export async function createUser(payload) {
  const { data } = await client.post('/api/User', payload)
  return data
}

export async function updateUser(id, payload) {
  const { data } = await client.put(`/api/User/${id}`, payload)
  return data
}

/** Soft delete: kayıt siliniyor değil, is_deleted = true yapılıyor. */
export async function deleteUser(id) {
  await client.delete(`/api/User/${id}`)
}

/** Kullanıcının rolleri + her yetkinin nereden geldiği. */
export async function fetchUserAccess(id) {
  const { data } = await client.get(`/api/User/${id}/access`)
  return data
}

export async function saveUserAccess(id, payload) {
  const { data } = await client.put(`/api/User/${id}/access`, payload)
  return data
}

// --- Roller ve yetkiler ---

export async function fetchRoles() {
  const { data } = await client.get('/api/Role')
  return data
}

export async function fetchPermissions() {
  const { data } = await client.get('/api/Role/permissions')
  return data
}

export async function createRole(payload) {
  const { data } = await client.post('/api/Role', payload)
  return data
}

export async function updateRole(id, payload) {
  const { data } = await client.put(`/api/Role/${id}`, payload)
  return data
}

export async function deleteRole(id) {
  await client.delete(`/api/Role/${id}`)
}

/** Giriş yapan kullanıcının kendi etkin yetkileri. */
export async function fetchCurrentUser() {
  const { data } = await client.get('/api/User/me')
  return data
}

// --- Coğrafi yetki (çizim alanı) ---

/** @param {'user'|'role'} target */
export async function fetchGeoArea(target, id) {
  const { data } = await client.get(`/api/GeoPermission/${target}/${id}`)
  return data
}

/**
 * @param {'user'|'role'} target
 * @param {{name: string, wkt: string|null}} payload wkt null ise alan kaldırılır
 */
export async function saveGeoArea(target, id, payload) {
  const { data } = await client.put(`/api/GeoPermission/${target}/${id}`, payload)
  return data
}
