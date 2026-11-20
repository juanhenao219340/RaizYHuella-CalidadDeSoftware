import { useEffect, useRef, useState } from 'react'
import { Outlet, useNavigate } from 'react-router-dom'
import NavMenu from './NavMenu'
// Pure helper import from Services/ (format.js): no network calls in this component.
import { formatRole } from '../Services/format'

// Top bar and page container. Receives session data and logout through props.
function AppLayout({ user = null, role = null, onLogout }) {
  const navigate = useNavigate()
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false)
  const mobileButtonRef = useRef(null)
  const userButtonRef = useRef(null)
  const roleLabel = formatRole(role)

  useEffect(() => {
    if (!isMobileMenuOpen && !isUserMenuOpen) {
      return undefined
    }

    function handleKeyDown(event) {
      if (event.key !== 'Escape') return
      if (isMobileMenuOpen) {
        setIsMobileMenuOpen(false)
        mobileButtonRef.current?.focus()
      }
      if (isUserMenuOpen) {
        setIsUserMenuOpen(false)
        userButtonRef.current?.focus()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isMobileMenuOpen, isUserMenuOpen])

  function closeMobileMenu() {
    setIsMobileMenuOpen(false)
    mobileButtonRef.current?.focus()
  }

  function handleLogout() {
    setIsMobileMenuOpen(false)
    setIsUserMenuOpen(false)
    onLogout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="min-h-dvh">
      <header className="border-b border-border bg-surface text-text">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-2">
          <div className="flex items-center gap-2">
            <img
              src="/logo.svg"
              alt="Logotipo de Raíz y Huella"
              className="h-16 w-16 max-md:h-12 max-md:w-12"
            />
            <span className="font-bold">Raíz y Huella</span>
          </div>

          <nav aria-label="Principal" className="hidden md:block">
            <NavMenu role={role} />
          </nav>

          <div className="relative hidden md:block">
            <button
              type="button"
              ref={userButtonRef}
              aria-expanded={isUserMenuOpen}
              aria-haspopup="true"
              onClick={() => setIsUserMenuOpen((open) => !open)}
              className="min-h-11 rounded-md px-2 font-semibold hover:text-primary"
            >
              {user?.name} ▾
            </button>
            {isUserMenuOpen && (
              <div className="absolute right-0 z-10 mt-2 w-56 rounded-md border border-border bg-surface-muted p-3 shadow">
                <p className="font-semibold">{user?.name}</p>
                <p className="text-sm">{roleLabel}</p>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="mt-3 min-h-11 w-full rounded-md border border-border px-3 text-left hover:text-primary"
                >
                  Cerrar sesión
                </button>
              </div>
            )}
          </div>

          <button
            type="button"
            ref={mobileButtonRef}
            aria-expanded={isMobileMenuOpen}
            aria-controls="mobile-menu"
            onClick={() => setIsMobileMenuOpen((open) => !open)}
            className="min-h-11 min-w-11 rounded-md px-3 md:hidden"
          >
            {isMobileMenuOpen ? 'Cerrar menú' : 'Menú'}
          </button>
        </div>

        {isMobileMenuOpen && (
          <div id="mobile-menu" className="border-t border-border bg-surface px-4 py-3 md:hidden">
            <nav aria-label="Principal móvil">
              <NavMenu role={role} orientation="vertical" onItemClick={closeMobileMenu} />
            </nav>
            <div className="mt-3 border-t border-border pt-3">
              <p className="font-semibold">
                {user?.name} · {roleLabel}
              </p>
              <button
                type="button"
                onClick={handleLogout}
                className="mt-2 min-h-11 w-full rounded-md border border-border px-3 text-left hover:text-primary"
              >
                Cerrar sesión
              </button>
            </div>
          </div>
        )}
      </header>

      <main className="mx-auto max-w-6xl p-4 md:p-6">
        <Outlet />
      </main>
    </div>
  )
}

export default AppLayout
