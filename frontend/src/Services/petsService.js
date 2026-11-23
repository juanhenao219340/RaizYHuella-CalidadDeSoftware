import { request } from './apiClient'


export function listPets() {
  return request('/api/pets')
}

export function getPet(id) {
  return request(`/api/pets/${id}`)
}

export function createPet({ name, species, breed, birthDate }) {
  return request('/api/pets', {
    method: 'POST',
    body: { name, species, breed, birthDate },
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
