import { request } from './apiClient'

// Service catalog. Administrator creates, edits and deletes; every authenticated user lists.
export function listServices() {
  return request('/api/services')
}

export function createService({ name, type }) {
  return request('/api/services', { method: 'POST', body: { name, type } })
}

export function updateService(id, { name, type }) {
  return request(`/api/services/${id}`, { method: 'PUT', body: { name, type } })
}

export function deleteService(id) {
  return request(`/api/services/${id}`, { method: 'DELETE' })
}
