


export const MAX_EMAIL_LENGTH = 256
export const MAX_PASSWORD_MESSAGE =
  'La contraseña debe tener al menos 8 caracteres, con mayúscula, minúscula y dígito.'
export const MAX_REASON_LENGTH = 500
export const DEL_PREFIX = 'DEL-'

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const ISO_DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/
const REFERENCE_PATTERN = /^[A-Za-z0-9-]*$/
const PASSWORD_UPPER = /[A-Z]/
const PASSWORD_LOWER = /[a-z]/
const PASSWORD_DIGIT = /\d/
const SERVICE_TYPES = ['Clinic', 'Aesthetics', 'Recreation']

export function cleanText(value) {
  return typeof value === 'string' ? value.trim() : ''
}

function addIfInvalid(errors, field, condition, message) {
  if (condition && !errors[field]) {
    errors[field] = message
  }
}

export function validateEmail(value) {
  const email = cleanText(value)
  if (!email) return 'El correo es obligatorio.'
  if (email.length > MAX_EMAIL_LENGTH) return 'El correo no puede superar 256 caracteres.'
  if (!EMAIL_PATTERN.test(email)) return 'El correo no tiene un formato válido.'
  return ''
}

export function validateLogin({ email, password }) {
  const errors = {}
  errors.email = validateEmail(email)
  addIfInvalid(errors, 'password', !password, 'La contraseña es obligatoria.')
  return compact(errors)
}


export function compact(errors) {
  return Object.fromEntries(Object.entries(errors).filter(([, message]) => Boolean(message)))
}

function validateDelPrefix(identifier) {
  return identifier.startsWith(DEL_PREFIX)
    ? 'El valor no puede empezar por DEL-, prefijo reservado por el sistema.'
    : ''
}

export function validatePassword(value) {
  if (!value) return 'La contraseña es obligatoria.'
  if (value.length < 8 || !PASSWORD_UPPER.test(value) || !PASSWORD_LOWER.test(value) || !PASSWORD_DIGIT.test(value)) {
    return MAX_PASSWORD_MESSAGE
  }
  return ''
}


function validateContactFields(values, errors) {
  const name = cleanText(values.name)
  errors.name = !name
    ? 'El nombre es obligatorio.'
    : name.length > 150
      ? 'El nombre no puede superar 150 caracteres.'
      : ''
  errors.email = validateEmail(values.email)
  addIfInvalid(errors, 'phone', cleanText(values.phone).length > 30, 'El teléfono no puede superar 30 caracteres.')
}

export function validateOwnerCreate(values) {
  const errors = {}
  const identification = cleanText(values.identification)
  errors.identification = !identification
    ? 'La identificación es obligatoria.'
    : identification.length > 20
      ? 'La identificación no puede superar 20 caracteres.'
      : validateDelPrefix(identification)
  validateContactFields(values, errors)
  errors.password = validatePassword(values.password)
  return compact(errors)
}

export function validateOwnerUpdate(values) {
  const errors = {}
  validateContactFields(values, errors)
  return compact(errors)
}

export function validateProfile(values) {
  return validateOwnerUpdate(values)
}

export function validatePetCreate(values) {
  const errors = {}
  validatePetFields(values, errors)
  return compact(errors)
}

export function validatePetUpdate(values) {
  const errors = {}
  validatePetFields(values, errors)
  return compact(errors)
}

function validatePetFields(values, errors) {
  const name = cleanText(values.name)
  errors.name = !name
    ? 'El nombre de la mascota es obligatorio.'
    : name.length > 100
      ? 'El nombre no puede superar 100 caracteres.'
      : ''

  const species = cleanText(values.species)
  errors.species = !species
    ? 'La especie es obligatoria.'
    : species.length > 50
      ? 'La especie no puede superar 50 caracteres.'
      : ''

  const breed = cleanText(values.breed)
  errors.breed = !breed
    ? 'La raza es obligatoria.'
    : breed.length > 100
      ? 'La raza no puede superar 100 caracteres.'
      : ''


  const birthDate = cleanText(values.birthDate)
  errors.birthDate = !birthDate || !isValidIsoDate(birthDate)
    ? 'La fecha de nacimiento es obligatoria.'
    : ''
}

export function isValidIsoDate(value) {
  if (!ISO_DATE_PATTERN.test(value)) return false
  const [year, month, day] = value.split('-').map(Number)
  const date = new Date(Date.UTC(year, month - 1, day))
  return date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day
}

export function validateService(values) {
  const errors = {}
  const name = cleanText(values.name)
  errors.name = !name
    ? 'El nombre del servicio es obligatorio.'
    : name.length > 100
      ? 'El nombre no puede superar 100 caracteres.'
      : ''
  addIfInvalid(errors, 'type', !SERVICE_TYPES.includes(values.type), 'El tipo de servicio es obligatorio.')
  return compact(errors)
}

export function validateBooking({ petId, serviceId, date, startTime }) {
  const errors = {}
  addIfInvalid(errors, 'petId', !petId, 'La mascota es obligatoria.')
  addIfInvalid(errors, 'serviceId', !serviceId, 'El servicio es obligatorio.')
  addIfInvalid(errors, 'date', !date, 'La fecha de la cita es obligatoria.')
  addIfInvalid(errors, 'startTime', !startTime, 'La hora de inicio es obligatoria.')
  return compact(errors)
}


export function validateReason(value, kind = 'cancel') {
  const reason = cleanText(value)
  if (!reason) {
    return kind === 'reschedule'
      ? 'El motivo del cambio es obligatorio.'
      : 'El motivo de la anulación es obligatorio.'
  }
  if (reason.length > MAX_REASON_LENGTH) {
    return 'El motivo no puede superar 500 caracteres.'
  }
  return ''
}


export function validateAppointmentFilters({ referenceNumber, petName, ownerIdentification }, { showOwnerFilter = false } = {}) {
  const errors = {}
  const reference = cleanText(referenceNumber)
  addIfInvalid(
    errors,
    'referenceNumber',
    reference.length > 20 || !REFERENCE_PATTERN.test(reference),
    'El radicado solo puede contener letras, números y guiones, y no superar 20 caracteres.',
  )
  addIfInvalid(
    errors,
    'petName',
    cleanText(petName).length > 100,
    'El nombre de la mascota no puede superar 100 caracteres.',
  )
  if (showOwnerFilter) {
    addIfInvalid(
      errors,
      'ownerIdentification',
      cleanText(ownerIdentification).length > 20,
      'La identificación del propietario no puede superar 20 caracteres.',
    )
  }
  return compact(errors)
}
