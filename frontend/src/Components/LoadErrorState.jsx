import Button from './Button'
// Pure constant import from Services/ (errorMessages.js): no network calls in this component.
import { LIST_LOAD_ERROR_MESSAGE } from '../Services/errorMessages'

// Shown when a list cannot be loaded. "Reintentar" repeats the load.
function LoadErrorState({ onRetry, message = LIST_LOAD_ERROR_MESSAGE }) {
  return (
    <div role="alert" className="flex flex-col items-start gap-3 rounded-lg border border-danger bg-surface-muted p-4">
      <p className="font-semibold text-danger">{message}</p>
      <Button variant="secondary" onClick={onRetry}>
        Reintentar
      </Button>
    </div>
  )
}

export default LoadErrorState
