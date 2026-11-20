import { config, CONFIG_INCOMPLETE_MESSAGE, isConfigComplete } from './config'
import {
  getFieldErrors,
  getProblemMessage,
  NETWORK_ERROR_MESSAGE,
} from './errorMessages'

// Error raised for every failed API call. `message` is the Spanish text for the UI.
export class ApiError extends Error {
  constructor({ status, message, code = null, fieldErrors = {} }) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.fieldErrors = fieldErrors
  }
}

// Token and 401 hooks are registered by the session layer, which avoids import cycles.
let tokenProvider = () => null
let unauthorizedHandler = null

export function registerTokenProvider(provider) {
  tokenProvider = provider
}

export function registerUnauthorizedHandler(handler) {
  unauthorizedHandler = handler
}

// Basic credentials: UTF-8 first, then base64 (btoa alone fails on non-ASCII text).
export function encodeBasicCredentials(username, password) {
  const bytes = new TextEncoder().encode(`${username}:${password}`)
  let binary = ''
  bytes.forEach((byte) => {
    binary += String.fromCharCode(byte)
  })
  return btoa(binary)
}

function buildHeaders(auth) {
  const headers = { Accept: 'application/json' }

  if (auth === 'basic') {
    headers.Authorization = `Basic ${encodeBasicCredentials(
      config.basicAuthUsername,
      config.basicAuthPassword,
    )}`
    return headers
  }

  const token = tokenProvider()
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }
  return headers
}

async function readBody(response) {
  try {
    return await response.json()
  } catch {
    // Non-JSON bodies (proxy pages, empty 401) fall back to the status-based message.
    return null
  }
}

// Single HTTP entry point. Components never build headers or URLs.
export async function request(
  path,
  { method = 'GET', body, auth = 'bearer', signal, skipUnauthorizedHandler = false } = {},
) {
  if (!isConfigComplete()) {
    throw new ApiError({ status: 0, message: CONFIG_INCOMPLETE_MESSAGE })
  }

  const headers = buildHeaders(auth)
  const init = { method, headers, signal }

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
    init.body = JSON.stringify(body)
  }

  let response
  try {
    response = await fetch(`${config.apiBaseUrl}${path}`, init)
  } catch (error) {
    if (error?.name === 'AbortError') {
      throw error
    }
    throw new ApiError({ status: 0, message: NETWORK_ERROR_MESSAGE })
  }

  if (response.status === 204) {
    return null
  }

  const problem = await readBody(response)

  if (!response.ok) {
    if (response.status === 401 && auth !== 'basic' && !skipUnauthorizedHandler) {
      unauthorizedHandler?.()
    }
    throw new ApiError({
      status: response.status,
      message: getProblemMessage(response.status, problem),
      code: problem?.code ?? null,
      fieldErrors: getFieldErrors(problem),
    })
  }

  return problem
}
