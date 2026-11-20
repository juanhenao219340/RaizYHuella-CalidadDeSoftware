function Spinner({ label = 'Cargando…' }) {
  return (
    <p role="status" className="flex items-center gap-2 p-4 text-text">
      <span
        aria-hidden="true"
        className="inline-block h-4 w-4 animate-spin rounded-full border-2 border-border border-t-primary"
      />
      {label}
    </p>
  )
}

export default Spinner
