import { NavLink } from 'react-router-dom'

// Sections per role (plan 5 bis.4). Administrators have no scheduling entry.
const NAV_ITEMS = {
  Administrator: [
    { to: '/admin/propietarios', label: 'Propietarios' },
    { to: '/admin/servicios', label: 'Servicios' },
    { to: '/admin/mascotas', label: 'Mascotas' },
    { to: '/citas', label: 'Citas' },
  ],
  Owner: [
    { to: '/propietario/mascotas', label: 'Mis mascotas' },
    { to: '/propietario/agendar', label: 'Agendar' },
    { to: '/citas', label: 'Mis citas' },
    { to: '/propietario/perfil', label: 'Perfil' },
  ],
}

const LINK_BASE = 'min-h-11 inline-flex items-center px-2 text-sm'
const LINK_ACTIVE = 'font-semibold text-primary underline underline-offset-8'
const LINK_IDLE = 'text-text hover:text-primary'

// Navigation list for one role. Only the sections of that role are rendered.
function NavMenu({ role = null, orientation = 'horizontal', onItemClick }) {
  const items = NAV_ITEMS[role] ?? []
  const listClass = orientation === 'vertical' ? 'flex flex-col' : 'flex items-center gap-1'

  return (
    <ul className={listClass}>
      {items.map((item) => (
        <li key={item.to}>
          <NavLink
            to={item.to}
            onClick={onItemClick}
            className={({ isActive }) =>
              `${LINK_BASE} ${isActive ? LINK_ACTIVE : LINK_IDLE}`
            }
          >
            {item.label}
          </NavLink>
        </li>
      ))}
    </ul>
  )
}

export default NavMenu
