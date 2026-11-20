import { ApiError, registerTokenProvider, request } from './apiClient'
import { INVALID_CREDENTIALS_MESSAGE } from './errorMessages'

const LOCKED_OUT_FALLBACK_MESSAGE = 'Cuenta bloqueada por intentos fallidos.'

// Storage key confirmed by the user (plan P-E). Only this module reads or writes it.
export const SESSION_STORAGE_KEY = 'veterinaria.session'

const ADMIN_HOME = '/admin/propietarios'
const OWNER_HOME = '/propietario/mascotas'

export function getHomePath(role) {
  return role === 'Administrator' ? ADMIN_HOME : OWNER_HOME
}

export function readStoredSession() {
  try {
    const raw = window.localStorage.getItem(SESSION_STORAGE_KEY)
    if (!raw) {
      return null
    }
    const parsed = JSON.parse(raw)
    if (typeof parsed?.token !== 'string' || typeof parsed?.expiresAt !== 'string') {
      return null
    }
    return { token: parsed.token, expiresAt: parsed.expiresAt }
  } catch {
    return null
  }
}

export function writeStoredSession({ token, expiresAt }) {
  window.localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({ token, expiresAt }))
}

export function clearStoredSession() {
  window.localStorage.removeItem(SESSION_STORAGE_KEY)
}

function readStoredToken() {
  return readStoredSession()?.token ?? null
}

registerTokenProvider(readStoredToken)

// Local clock check is only a convenience; the server decides validity (plan 3.2).
function isExpired(expiresAt) {
  const timestamp = Date.parse(expiresAt)
  return Number.isNaN(timestamp) || timestamp <= Date.now()
}

// GET /api/auth/me. Returns the identity used for routing and display.
export async function me({ skipUnauthorizedHandler = false } = {}) {
  const response = await request('/api/auth/me', { skipUnauthorizedHandler })
  return {
    id: response.userId,
    role: response.role,
    email: response.email,
    name: response.name,
  }
}

// Login with Basic credentials, store the token, then load the identity.
export async function login(email, password) {
  let session
  try {
    session = await request('/api/auth/login', {
      method: 'POST',
      auth: 'basic',
      body: { email, password },
      skipUnauthorizedHandler: true,
    })
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      throw new ApiError({ status: 401, message: INVALID_CREDENTIALS_MESSAGE })
    }
    // RN-05 lockout (R15-1): keep the server's detail, which gives the remaining minutes and seconds.
    if (error instanceof ApiError && error.status === 423 && error.code === 'RN-05') {
      throw new ApiError({ status: 423, message: error.message || LOCKED_OUT_FALLBACK_MESSAGE, code: 'RN-05' })
    }
    throw error
  }

  writeStoredSession({ token: session.token, expiresAt: session.expiresAt })

  try {
    return await me()
  } catch (error) {
    clearStoredSession()
    throw error
  }
}

// Restores a session on page load. Returns the user, or null when there is no valid session.
export async function restoreSession() {
  const stored = readStoredSession()
  if (!stored) {
    return null
  }

  if (isExpired(stored.expiresAt)) {
    clearStoredSession()
    return null
  }

  try {
    return await me({ skipUnauthorizedHandler: true })
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      clearStoredSession()
    }
    return null
  }
}
