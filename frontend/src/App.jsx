import { Navigate, Route, Routes } from 'react-router-dom'
import RequireAuth from './Components/RequireAuth'
import RequireRole from './Components/RequireRole'
import AppLayout from './Components/AppLayout'
import AppointmentsPage from './Pages/AppointmentsPage'
import BookAppointmentPage from './Pages/BookAppointmentPage'
import LoginPage from './Pages/LoginPage'
import NotFoundPage from './Pages/NotFoundPage'
import OwnersPage from './Pages/OwnersPage'
import PetsPage from './Pages/PetsPage'
import PetsReadOnlyPage from './Pages/PetsReadOnlyPage'
import ProfilePage from './Pages/ProfilePage'
import ServicesPage from './Pages/ServicesPage'
import { useSession } from './Context/SessionContext'
import { getHomePath } from './Services/authService'

const ADMIN = ['Administrator']
const OWNER = ['Owner']
const BOTH_ROLES = ['Administrator', 'Owner']


function App() {
  const { user, role, isRestoring, logout } = useSession()


  function guard(allowedRoles, page) {
    return (
      <RequireRole user={user} role={role} allowedRoles={allowedRoles}>
        {page}
      </RequireRole>
    )
  }

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <RequireAuth user={user} isRestoring={isRestoring}>
            <AppLayout user={user} role={role} onLogout={logout} />
          </RequireAuth>
        }
      >
        <Route path="/" element={<Navigate to={getHomePath(role)} replace />} />

        <Route path="/admin/propietarios" element={guard(ADMIN, <OwnersPage />)} />
        <Route path="/admin/servicios" element={guard(ADMIN, <ServicesPage />)} />
        <Route path="/admin/mascotas" element={guard(ADMIN, <PetsReadOnlyPage />)} />

        <Route path="/propietario/mascotas" element={guard(OWNER, <PetsPage />)} />
        <Route path="/propietario/agendar" element={guard(OWNER, <BookAppointmentPage />)} />
        <Route path="/propietario/perfil" element={guard(OWNER, <ProfilePage />)} />

        <Route path="/citas" element={guard(BOTH_ROLES, <AppointmentsPage />)} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}

export default App
