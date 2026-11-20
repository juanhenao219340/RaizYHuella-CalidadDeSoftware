function FormErrorBanner({ message = '' }) {
  if (!message) {
    return null
  }

  return (
    <p role="alert" className="rounded-md border border-danger bg-surface-muted p-3 text-danger">
      {message}
    </p>
  )
}

export default FormErrorBanner
