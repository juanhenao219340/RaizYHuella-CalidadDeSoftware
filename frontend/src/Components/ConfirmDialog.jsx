import Button from './Button'
import FormErrorBanner from './FormErrorBanner'
import Modal from './Modal'

// Confirmation before a destructive action. The parent performs the request and passes errors back.
function ConfirmDialog({
  title,
  message,
  confirmLabel,
  onConfirm,
  onCancel,
  isBusy = false,
  error = '',
}) {
  return (
    <Modal title={title} onClose={onCancel}>
      <p>{message}</p>
      <div className="mt-4">
        <FormErrorBanner message={error} />
      </div>
      <div className="mt-6 flex justify-end gap-3">
        <Button variant="secondary" onClick={onCancel} disabled={isBusy}>
          Cancelar
        </Button>
        <Button variant="danger" onClick={onConfirm} isLoading={isBusy} disabled={isBusy}>
          {isBusy ? 'Procesando…' : confirmLabel}
        </Button>
      </div>
    </Modal>
  )
}

export default ConfirmDialog
