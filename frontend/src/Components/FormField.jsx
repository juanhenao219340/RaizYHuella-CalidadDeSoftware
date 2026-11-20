const CONTROL_BASE =
  'min-h-11 w-full rounded-md border bg-surface px-3 py-2 text-text disabled:opacity-60'

// Label, control and field message. The control receives id and ARIA attributes here.
function FormField({ id, label, error = '', hint = '', as = 'input', children = null, ...controlProps }) {
  const Control = as
  const hintId = `${id}-hint`
  const errorId = `${id}-error`
  const describedBy = [hint ? hintId : null, error ? errorId : null].filter(Boolean).join(' ') || undefined

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="font-semibold text-text">
        {label}
      </label>
      <Control
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        className={`${CONTROL_BASE} ${error ? 'border-danger' : 'border-border'}`}
        {...controlProps}
      >
        {children}
      </Control>
      {hint && (
        <p id={hintId} className="text-sm">
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
    </div>
  )
}

export default FormField
