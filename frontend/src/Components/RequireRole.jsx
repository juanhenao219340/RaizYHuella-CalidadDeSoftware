import { Navigate } from 'react-router-dom'

import { getHomePath } from '../Services/authService'


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
