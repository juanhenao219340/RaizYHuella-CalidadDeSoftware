import Spinner from './Spinner'
import { Navigate } from 'react-router-dom'


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
