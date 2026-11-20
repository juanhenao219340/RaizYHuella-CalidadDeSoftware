// Build-time configuration read from VITE_* variables (plan 3.6).
// Vite inlines these values at build time. They are never displayed or logged.
const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/+$/, '')
const basicAuthUsername = import.meta.env.VITE_BASIC_AUTH_USERNAME ?? ''
const basicAuthPassword = import.meta.env.VITE_BASIC_AUTH_PASSWORD ?? ''

export const CONFIG_INCOMPLETE_MESSAGE = 'La configuración de la aplicación está incompleta.'

export const config = Object.freeze({
  apiBaseUrl,
  basicAuthUsername,
  basicAuthPassword,
})

export function isConfigComplete() {
  return Boolean(apiBaseUrl && basicAuthUsername && basicAuthPassword)
}
