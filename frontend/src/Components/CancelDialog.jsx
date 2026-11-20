import { useState } from 'react'
import Button from './Button'
import FormErrorBanner from './FormErrorBanner'
import FormField from './FormField'
import Modal from './Modal'
import { validateReason } from '../Services/validation'

// Administrator cancels a Confirmed appointment. A reason is required (CA-PRO-05).
// Presentation only: the page owns the request, isSubmitting, server errors and the list reload.
function CancelDialog({ appointment, isSubmitting = false, formError = '', reasonError = '', onConfirm, onClose }) {
  const [reason, setReason] = useState('')
  const [localError, setLocalError] = useState('')

  function handleSubmit(event) {
    event.preventDefault()
    if (isSubmitting) return

    const error = validateReason(reason, 'cancel')
    setLocalError(error)
    if (error) return

    onConfirm({ reason: reason.trim() })
  }

  return (
    <Modal title={`¿Anular ${appointment.referenceNumber}?`} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
        <fieldset disabled={isSubmitting} aria-busy={isSubmitting || undefined} className="flex flex-col gap-4">
          <FormField
            id="cancel-reason"
            as="textarea"
            rows={4}
            label="Motivo (obligatorio)"
            value={reason}
            onChange={(event) => {
              setReason(event.target.value)
              setLocalError('')
            }}
            error={localError || reasonError}
          />

          <FormErrorBanner message={formError} />

          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={onClose}>
              Cancelar
            </Button>
            <Button type="submit" variant="danger" isLoading={isSubmitting}>
              Anular
            </Button>
          </div>
        </fieldset>
      </form>
    </Modal>
  )
}

export default CancelDialog
