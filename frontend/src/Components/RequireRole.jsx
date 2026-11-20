import { Navigate } from 'react-router-dom'
// Pure helper import from Services/ (authService.getHomePath): no network calls in this component.
import { getHomePath } from '../Services/authService'

// Role guard. Sends a user without the required role to the home of their own role (C-LOG-7).
function RequireRole({ user = null, role = null, allowedRoles = [], children }) {
  if (!user || !role) {
    return <Navigate to="/login" replace />
  }

  if (!allowedRoles.includes(role)) {
    return <Navigate to={getHomePath(role)} replace />
  }

  return children
}

export default RequireRole
