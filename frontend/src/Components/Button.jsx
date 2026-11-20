const VARIANT_STYLES = {
  primary: 'bg-primary text-surface hover:bg-primary-dark',
  secondary: 'border border-border bg-surface text-text hover:bg-surface-muted',
  danger: 'bg-danger text-surface hover:opacity-90',
  ghost: 'text-primary hover:underline',
}

const BASE_STYLE =
  'min-h-11 inline-flex items-center justify-center gap-2 rounded-md px-4 font-semibold disabled:cursor-not-allowed disabled:opacity-60'

function Button({ type = 'button', variant = 'primary', isLoading = false, className = '', children, ...rest }) {
  return (
    <button
      type={type}
      aria-busy={isLoading || undefined}
      className={`${BASE_STYLE} ${VARIANT_STYLES[variant] ?? VARIANT_STYLES.primary} ${className}`}
      {...rest}
    >
      {children}
    </button>
  )
}

export default Button
