import { request } from './apiClient'

// Pet endpoints. Each function sends only the fields the API accepts; `ownerId` is never sent (RF-PRO-09).
export function listPets() {
  return request('/api/pets')
}

export function getPet(id) {
  return request(`/api/pets/${id}`)
}

export function createPet({ identifier, name, species, breed, birthDate }) {
  return request('/api/pets', {
    method: 'POST',
    body: { identifier, name, species, breed, birthDate },
  })
}

export function updatePet(id, { name, species, breed, birthDate }) {
  return request(`/api/pets/${id}`, {
    method: 'PUT',
    body: { name, species, breed, birthDate },
  })
}

export function deletePet(id) {
  return request(`/api/pets/${id}`, { method: 'DELETE' })
}
