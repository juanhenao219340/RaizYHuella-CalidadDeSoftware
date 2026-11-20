import Spinner from './Spinner'
import { Navigate } from 'react-router-dom'

// Guard for protected routes. Receives session data as props; it does not read the context.
function RequireAuth({ user = null, isRestoring = false, children }) {
  if (isRestoring) {
    return <Spinner label="Cargando la sesión…" />
  }

  if (!user) {
    return <Navigate to="/login" replace />
  }

  return children
}

export default RequireAuth
