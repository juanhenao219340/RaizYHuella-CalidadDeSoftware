function EmptyState({ message, children = null }) {
  return (
    <div className="rounded-lg border border-dashed border-border bg-surface-muted p-6 text-center">
      <p>{message}</p>
      {children && <div className="mt-4 flex justify-center">{children}</div>}
    </div>
  )
}

export default EmptyState
