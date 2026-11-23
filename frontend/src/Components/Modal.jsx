import { useEffect, useId, useRef } from 'react'

const FOCUSABLE_SELECTOR =
  'button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled])'


function Modal({ title, onClose, children }) {
  const titleId = useId()
  const dialogRef = useRef(null)
  const openerRef = useRef(null)

  useEffect(() => {
    const opener = document.activeElement
    openerRef.current = opener
    const dialog = dialogRef.current
    if (dialog && !dialog.contains(document.activeElement)) {
      dialog.focus()
    }
    return () => {
      if (opener && typeof opener.focus === 'function') {
        opener.focus()
      }
    }
  }, [])

  useEffect(() => {
    function handleKeyDown(event) {
      if (event.key === 'Escape') {
        onClose()
        return
      }
      if (event.key !== 'Tab' || !dialogRef.current) return

      const focusable = dialogRef.current.querySelectorAll(FOCUSABLE_SELECTOR)
      if (focusable.length === 0) return
      const first = focusable[0]
      const last = focusable[focusable.length - 1]

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  return (
    <div className="fixed inset-0 z-20 flex items-center justify-center bg-text/50 p-4">
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className="max-h-[calc(100dvh-2rem)] w-full max-w-lg overflow-y-auto rounded-lg border border-border bg-surface p-6 shadow-lg"
      >
        <div className="flex items-start justify-between gap-4">
          <h2 id={titleId} className="text-xl font-bold">
            {title}
          </h2>
          <button
            type="button"
            aria-label="Cerrar"
            onClick={onClose}
            className="min-h-11 min-w-11 rounded-md px-2 hover:text-primary"
          >
            <span aria-hidden="true">✕</span>
          </button>
        </div>
        <div className="mt-4">{children}</div>
      </div>
    </div>
  )
}

export default Modal
