import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import Button from '../Components/Button'
import FormErrorBanner from '../Components/FormErrorBanner'
import FormField from '../Components/FormField'
import { useSession } from '../Context/SessionContext'
import { getHomePath } from '../Services/authService'
import { CONFIG_INCOMPLETE_MESSAGE, isConfigComplete } from '../Services/config'
import { SESSION_EXPIRED_MESSAGE } from '../Services/errorMessages'
import { validateLogin } from '../Services/validation'

function LoginPage() {
  const navigate = useNavigate()
  const { user, login, sessionExpired } = useSession()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (user) {
    return <Navigate to={getHomePath(user.role)} replace />
  }

  const isConfigReady = isConfigComplete()

  async function handleSubmit(event) {
    event.preventDefault()
    if (isSubmitting || !isConfigReady) return

    setError('')
    const normalizedEmail = email.trim()
    const errors = validateLogin({ email: normalizedEmail, password })
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    setIsSubmitting(true)
    try {
      const loggedUser = await login(normalizedEmail, password)
      navigate(getHomePath(loggedUser.role), { replace: true })
    } catch (err) {
      setError(err.message || 'No fue posible iniciar sesión.')
      setPassword('')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="flex min-h-dvh flex-col items-center justify-center p-4">
      <div className="flex flex-col items-center gap-2">
        <img src="/gemini-svg.svg" alt="Logotipo de Raíz y Huella" width="48" height="48" />
        <p className="text-lg font-bold text-text">Raíz y Huella</p>
      </div>

      <section className="mt-6 w-full max-w-md rounded-lg border border-border bg-surface-muted p-6">
        <h1 className="text-2xl font-bold text-text">Iniciar sesión</h1>

        {sessionExpired && (
          <p role="alert" className="mt-4 rounded-md border border-warning bg-surface p-3 font-semibold text-warning">
            {SESSION_EXPIRED_MESSAGE}
          </p>
        )}

        {!isConfigReady && (
          <div className="mt-4">
            <FormErrorBanner message={CONFIG_INCOMPLETE_MESSAGE} />
          </div>
        )}

        <form className="mt-4 flex flex-col gap-4" onSubmit={handleSubmit} noValidate>
          <fieldset disabled={isSubmitting || !isConfigReady} aria-busy={isSubmitting || undefined} className="flex flex-col gap-4">
            <FormField
              id="login-email"
              label="Correo electrónico"
              type="email"
              autoComplete="username"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              error={fieldErrors.email}
            />

            <FormField
              id="login-password"
              label="Contraseña"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              error={fieldErrors.password}
            />

            <FormErrorBanner message={error} />

            <Button type="submit" isLoading={isSubmitting} className="w-full">
              {isSubmitting ? 'Entrando…' : 'Entrar'}
            </Button>
          </fieldset>
        </form>
      </section>
    </main>
  )
}

export default LoginPage
