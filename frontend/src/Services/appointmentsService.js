import { request } from './apiClient'

// Appointment endpoints. The owner comes from the JWT; ownerId is never sent (RF-PRO-09).
export function listAppointments({ referenceNumber, petIdentifier, ownerIdentification, serviceType } = {}) {
  const params = new URLSearchParams()
  if (referenceNumber) params.set('referenceNumber', referenceNumber)
  if (petIdentifier) params.set('petIdentifier', petIdentifier)
  if (ownerIdentification) params.set('ownerIdentification', ownerIdentification)
  if (serviceType) params.set('serviceType', serviceType)
  const query = params.toString()
  return request(`/api/appointments${query ? `?${query}` : ''}`)
}

export function getAvailability(serviceId, date, { signal } = {}) {
  const params = new URLSearchParams({ serviceId, date })
  return request(`/api/appointments/availability?${params.toString()}`, { signal })
}

export function createAppointment({ petId, serviceId, date, startTime }) {
  return request('/api/appointments', {
    method: 'POST',
    body: { petId, serviceId, date, startTime },
  })
}

export function updateAppointment(id, { serviceId, date, startTime, reason }) {
  return request(`/api/appointments/${id}`, {
    method: 'PUT',
    body: { serviceId, date, startTime, reason },
  })
}

export function cancelAppointment(id, reason) {
  return request(`/api/appointments/${id}/cancel`, {
    method: 'POST',
    body: { reason },
  })
}
