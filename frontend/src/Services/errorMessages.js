export const GENERIC_ERROR_MESSAGE = 'Ocurrió un error inesperado.'
export const NETWORK_ERROR_MESSAGE = 'No se pudo conectar con el servidor. Intente de nuevo.'
export const INVALID_CREDENTIALS_MESSAGE = 'Credenciales no válidas.'
export const SESSION_EXPIRED_MESSAGE = 'Su sesión ha caducado. Inicie sesión de nuevo.'
export const LIST_LOAD_ERROR_MESSAGE = 'No se pudo cargar la lista.'

const FALLBACK_MESSAGES = {
  400: 'Los datos enviados no son válidos.',
  401: 'Sesión no válida o caducada.',
  403: 'No tiene permiso para realizar esta acción.',
  404: 'El recurso no existe o no está disponible.',
  409: 'Conflicto con el estado actual.',
  422: 'Regla de negocio incumplida.',
}

// Chooses the text shown to the user from a ProblemDetails body.
// The API is the source of truth for messages; they are already in Spanish.
export function getProblemMessage(status, problem) {
  // 404 uses title only: the API never reveals whether a resource exists (plan P8-4).
  if (status === 404 && problem?.title) {
    return problem.title
  }
  return problem?.detail || problem?.title || FALLBACK_MESSAGES[status] || GENERIC_ERROR_MESSAGE
}

// Converts an API error key to the camelCase field name used by the forms.
// Examples: "Name" -> "name", "$.type" -> "type", "$.items[0].price" -> "price".
export function toFieldName(key) {
  let name = String(key).replace(/^\$\./, '')
  if (/[.[\]]/.test(name)) {
    const segments = name.split(/[.[\]]+/).filter(Boolean)
    name = segments[segments.length - 1] ?? name
  }
  return name.charAt(0).toLowerCase() + name.slice(1)
}

// Maps ValidationProblemDetails.errors to one message per camelCase field (first message wins).
// Keys that are not form fields (such as "request") are kept; splitFieldErrors routes them to the banner.
export function getFieldErrors(problem) {
  const errors = problem?.errors
  if (!errors || typeof errors !== 'object') {
    return {}
  }

  return Object.entries(errors).reduce((result, [key, messages]) => {
    const first = Array.isArray(messages) ? messages[0] : messages
    const text = typeof first === 'string' ? first : first?.message
    if (text) {
      result[toFieldName(key)] = text
    }
    return result
  }, {})
}

// Separates server field errors into the ones that match a visible form field and the rest.
// Unmatched messages are joined into one text, to be shown in the form banner instead of being dropped.
export function splitFieldErrors(fieldErrors, knownFields) {
  const matched = {}
  const unmatched = []
  Object.entries(fieldErrors ?? {}).forEach(([field, message]) => {
    if (knownFields.includes(field)) {
      matched[field] = message
    } else {
      unmatched.push(message)
    }
  })
  return { fieldErrors: matched, unmatchedMessage: unmatched.join(' ') }
}

// Identifies which form field a 409 conflict (RF-REG-06) refers to, from its Spanish message.
export function getConflictField(error) {
  if (error?.status !== 409) {
    return null
  }
  const text = (error.message ?? '').toLowerCase()
  if (text.includes('identificación')) return 'identification'
  if (text.includes('correo')) return 'email'
  return null
}
