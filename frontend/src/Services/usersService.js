import { request } from './apiClient'

// User endpoints. Administrator lists and manages owners; any authenticated user reads and edits its own profile.
export function listOwners() {
  return request('/api/users')
}

export function getUser(id) {
  return request(`/api/users/${id}`)
}

// Creates an owner account. The role is not sent: the server always assigns Owner.
export function createOwner({ identification, name, email, phone, password }) {
  return request('/api/users', {
    method: 'POST',
    body: { identification, name, email, phone: phone || null, password },
  })
}

export function updateUser(id, { name, email, phone }) {
  return request(`/api/users/${id}`, {
    method: 'PUT',
    body: { name, email, phone: phone || null },
  })
}

export function deleteUser(id) {
  return request(`/api/users/${id}`, { method: 'DELETE' })
}

export function getMyProfile() {
  return request('/api/users/me')
}

export function updateMyProfile({ name, email, phone }) {
  return request('/api/users/me', {
    method: 'PUT',
    body: { name, email, phone: phone || null },
  })
}
