import { useState } from 'react'
import AvailabilityPicker from './AvailabilityPicker'
import Button from './Button'
import FormErrorBanner from './FormErrorBanner'
import FormField from './FormField'
import Modal from './Modal'
import Spinner from './Spinner'
// Pure helpers only (no network): date formatting and validation.
import { formatDate, todayIsoDate } from '../Services/format'
import { validateReason } from '../Services/validation'

// Administrator reschedules a Confirmed appointment. Only slots with capacity are offered (R13-12).
// Presentation only: the page loads services and availability, sends the request and owns the errors.
// The page passes slotsVersion; a new version means the slot list was reloaded and the chosen time is cleared (RN-03).
function RescheduleDialog({
  appointment,
  services = [],
  slots = [],
  isSlotsLoading = false,
  slotsError = '',
  slotsVersion = 0,
  isSubmitting = false,
  formError = '',
  reasonError = '',
  onScheduleChange,
  onConfirm,
  onClose,
}) {
  const [serviceId, setServiceId] = useState(appointment.serviceId)
  const [date, setDate] = useState(appointment.date)
  const [startTime, setStartTime] = useState('')
  const [reason, setReason] = useState('')
  const [localErrors, setLocalErrors] = useState({})
  const [seenSlotsVersion, setSeenSlotsVersion] = useState(slotsVersion)
  const today = todayIsoDate()

  // Clear the chosen time when the page reloads the slots after a stale-slot error (adjusting state during render).
  if (seenSlotsVersion !== slotsVersion) {
    setSeenSlotsVersion(slotsVersion)
    setStartTime('')
  }

  const availableSlots = slots.filter((slot) => slot.available)
  const reasonFieldError = localErrors.reason || reasonError

  function handleSubmit(event) {
    event.preventDefault()
    if (isSubmitting) return

    const errors = {}
    if (!serviceId) errors.serviceId = 'El servicio es obligatorio.'
    if (!date) errors.date = 'La fecha de la cita es obligatoria.'
    if (!startTime) errors.startTime = 'La hora de inicio es obligatoria.'
    const reasonMessage = validateReason(reason, 'reschedule')
    if (reasonMessage) errors.reason = reasonMessage
    setLocalErrors(errors)
    if (Object.keys(errors).length > 0) return

    onConfirm({ serviceId, date, startTime, reason: reason.trim() })
  }

  return (
    <Modal title={`Reprogramar ${appointment.referenceNumber}`} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
        <fieldset disabled={isSubmitting} aria-busy={isSubmitting || undefined} className="flex flex-col gap-4">
          <FormField
            id="reschedule-service"
            as="select"
            label="Servicio"
            value={serviceId}
            onChange={(event) => {
              const nextServiceId = event.target.value
              setServiceId(nextServiceId)
              setStartTime('')
              onScheduleChange({ serviceId: nextServiceId, date })
            }}
            error={localErrors.serviceId}
          >
            {services.map((service) => (
              <option key={service.id} value={service.id}>
                {service.name}
              </option>
            ))}
          </FormField>

          <FormField
            id="reschedule-date"
            label="Fecha"
            type="date"
            min={today}
            value={date}
            onChange={(event) => {
              const nextDate = event.target.value
              setDate(nextDate)
              setStartTime('')
              onScheduleChange({ serviceId, date: nextDate })
            }}
            error={localErrors.date}
          />

          <div className="flex flex-col gap-2">
            <span className="font-semibold">Hora de inicio</span>
            {isSlotsLoading ? (
              <Spinner label="Cargando las franjas…" />
            ) : slotsError ? (
              <FormErrorBanner message={slotsError} />
            ) : availableSlots.length === 0 ? (
              <p>No hay franjas disponibles para esta fecha. Pruebe otra fecha.</p>
            ) : (
              <AvailabilityPicker
                slots={availableSlots}
                value={startTime}
                onChange={setStartTime}
                idPrefix="reschedule-slot"
              />
            )}
            {localErrors.startTime && (
              <p className="text-sm font-semibold text-danger">{localErrors.startTime}</p>
            )}
          </div>

          <FormField
            id="reschedule-reason"
            as="textarea"
            rows={4}
            label="Motivo (obligatorio)"
            value={reason}
            onChange={(event) => {
              setReason(event.target.value)
              setLocalErrors((current) => ({ ...current, reason: undefined }))
            }}
            error={reasonFieldError}
          />

          <FormErrorBanner message={formError} />

          <p className="text-sm">Cita original: {formatDate(appointment.date)} {appointment.startTime}</p>

          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={onClose}>
              Cancelar
            </Button>
            <Button type="submit" isLoading={isSubmitting}>
              Guardar
            </Button>
          </div>
        </fieldset>
      </form>
    </Modal>
  )
}

export default RescheduleDialog
