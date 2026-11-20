import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import * as authService from '../Services/authService'
import { registerUnauthorizedHandler } from '../Services/apiClient'

const SessionContext = createContext(null)

// Holds only shared session state. Storage and HTTP stay in Services/.
export function SessionProvider({ children }) {
  const [user, setUser] = useState(null)
  const [isRestoring, setIsRestoring] = useState(true)
  const [sessionExpired, setSessionExpired] = useState(false)

  const logout = useCallback(() => {
    authService.clearStoredSession()
    setSessionExpired(false)
    setUser(null)
  }, [])

  // A 401 on a protected request ends the session and shows the expiry notice (plan 3.5).
  const expireSession = useCallback(() => {
    authService.clearStoredSession()
    setSessionExpired(true)
    setUser(null)
  }, [])

  useEffect(() => {
    registerUnauthorizedHandler(expireSession)
    return () => registerUnauthorizedHandler(null)
  }, [expireSession])

  const restoreSession = useCallback(async () => {
    setIsRestoring(true)
    try {
      const currentUser = await authService.restoreSession()
      setUser(currentUser)
    } finally {
      setIsRestoring(false)
    }
  }, [])

  useEffect(() => {
    restoreSession()
  }, [restoreSession])

  const login = useCallback(async (email, password) => {
    const currentUser = await authService.login(email, password)
    setSessionExpired(false)
    setUser(currentUser)
    return currentUser
  }, [])

  const value = useMemo(
    () => ({
      user,
      role: user?.role ?? null,
      isRestoring,
      sessionExpired,
      login,
      logout,
      restoreSession,
    }),
    [user, isRestoring, sessionExpired, login, logout, restoreSession],
  )

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
}

export function useSession() {
  const context = useContext(SessionContext)
  if (!context) {
    throw new Error('useSession debe usarse dentro de SessionProvider.')
  }
  return context
}
