// Labels and date helpers for display. Pure functions (no IO, no React). Spanish output.

export const ROLE_LABELS = {
  Administrator: 'Administrador',
  Owner: 'Propietario',
}

export const SERVICE_TYPE_LABELS = {
  Clinic: 'Clínica',
  Grooming: 'Peluquería',
  Recreation: 'Recreación',
}

export const APPOINTMENT_STATUS_LABELS = {
  Confirmed: 'Confirmada',
  Cancelled: 'Anulada',
}

export const SERVICE_TYPE_OPTIONS = Object.entries(SERVICE_TYPE_LABELS).map(([value, label]) => ({
  value,
  label,
}))

const dateFormatter = new Intl.DateTimeFormat('es-CO', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  timeZone: 'UTC',
})

export function formatRole(role) {
  return ROLE_LABELS[role] ?? ''
}

export function formatServiceType(type) {
  return SERVICE_TYPE_LABELS[type] ?? type ?? ''
}

export function formatAppointmentStatus(status) {
  return APPOINTMENT_STATUS_LABELS[status] ?? status ?? ''
}

// Converts an API date (yyyy-MM-dd) to dd/mm/aaaa. UTC avoids a one-day shift.
export function formatDate(isoDate) {
  if (!isoDate) return ''
  return dateFormatter.format(new Date(`${isoDate}T00:00:00Z`))
}

// Appointment date and start time: "25/11/2026 08:00".
export function formatDateTime(isoDate, startTime) {
  return [formatDate(isoDate), startTime].filter(Boolean).join(' ')
}

export function formatSlotCount(count) {
  return count === 1 ? '1 cupo' : `${count} cupos`
}

// Today in the browser's local calendar, as yyyy-MM-dd (used for the date min; the API decides).
export function todayIsoDate() {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}
