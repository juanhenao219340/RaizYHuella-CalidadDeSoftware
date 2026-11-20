import { useCallback, useEffect, useState } from 'react'
import AppointmentFilters from '../Components/AppointmentFilters'
import AppointmentsTable from '../Components/AppointmentsTable'
import CancelDialog from '../Components/CancelDialog'
import EmptyState from '../Components/EmptyState'
import LoadErrorState from '../Components/LoadErrorState'
import RescheduleDialog from '../Components/RescheduleDialog'
import { useSession } from '../Context/SessionContext'
import { cancelAppointment, getAvailability, listAppointments, updateAppointment } from '../Services/appointmentsService'
import { listServices } from '../Services/servicesService'
import { validateAppointmentFilters } from '../Services/validation'

const EMPTY_FILTERS = {
  referenceNumber: '',
  petIdentifier: '',
  ownerIdentification: '',
  serviceType: '',
}

// Codes after which the appointment is stale: the list is reloaded (RF-PRO-11, RF-PRO-07).
const STALE_APPOINTMENT_CODES = ['RF-PRO-11', 'RF-PRO-07']
// Codes after which the slot list is stale (RN-03 full slot, P-01 time already passed, RN-01 past date): slots are reloaded and the chosen time is cleared.
const STALE_SLOT_CODES = ['RN-03', 'RN-01', 'P-01']

// Trims filters and drops blanks: blank filters count as absent (plan section 5).
function cleanFilters(values) {
  return Object.fromEntries(
    Object.entries(values)
      .map(([key, value]) => [key, value.trim()])
      .filter(([, value]) => value !== ''),
  )
}

// Shows a dialog error: RN-04 (reason rule) goes under the reason field, anything else in the banner.
function showDialogError(err, fallbackMessage, setFormError, setReasonError) {
  if (err.code === 'RN-04') {
    setReasonError(err.message)
    return
  }
  setFormError(err.message || fallbackMessage)
}

// Appointments for both roles (RF-CON-01, RF-CON-05). The API decides what each user sees.
// Actions (reschedule, cancel) are shown only to the administrator.
// This page owns every request made by the dialogs: services, availability, cancel, reschedule.
function AppointmentsPage() {
  const { role } = useSession()
  const isAdmin = role === 'Administrator'
  const [draft, setDraft] = useState(EMPTY_FILTERS)
  const [applied, setApplied] = useState({})
  const [draftErrors, setDraftErrors] = useState({})
  const [appointments, setAppointments] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [selected, setSelected] = useState(null)
  const [dialogKind, setDialogKind] = useState(null) // 'reschedule' | 'cancel' | null
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [dialogError, setDialogError] = useState('')
  const [dialogReasonError, setDialogReasonError] = useState('')
  const [services, setServices] = useState([])
  const [scheduleQuery, setScheduleQuery] = useState(null) // { serviceId, date } chosen in the reschedule dialog
  const [slots, setSlots] = useState([])
  const [isSlotsLoading, setIsSlotsLoading] = useState(false)
  const [slotsError, setSlotsError] = useState('')
  const [slotsVersion, setSlotsVersion] = useState(0)

  const loadAppointments = useCallback(async (filters) => {
    setIsLoading(true)
    setLoadError('')
    try {
      setAppointments(await listAppointments(filters))
    } catch (err) {
      setLoadError(err.message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    loadAppointments(applied)
  }, [applied, loadAppointments])

  // Availability for the reschedule dialog. Reloads when the chosen service/date changes or after a stale slot.
  useEffect(() => {
    if (dialogKind !== 'reschedule' || !scheduleQuery?.serviceId || !scheduleQuery?.date) {
      setSlots([])
      return undefined
    }

    const controller = new AbortController()
    setIsSlotsLoading(true)
    setSlotsError('')
    getAvailability(scheduleQuery.serviceId, scheduleQuery.date, { signal: controller.signal })
      .then((result) => setSlots(result))
      .catch((err) => {
        if (err.name !== 'AbortError') {
          setSlots([])
          setSlotsError(err.message)
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setIsSlotsLoading(false)
        }
      })

    return () => controller.abort()
  }, [dialogKind, scheduleQuery, slotsVersion])

  function updateDraft(field, value) {
    setDraft((current) => ({ ...current, [field]: value }))
  }

  function handleSearch() {
    const errors = validateAppointmentFilters(draft)
    setDraftErrors(errors)
    if (Object.keys(errors).length > 0) return
    setApplied(cleanFilters(draft))
  }

  function handleClear() {
    setDraft(EMPTY_FILTERS)
    setDraftErrors({})
    setApplied({})
  }

  async function loadServices() {
    try {
      setServices(await listServices())
    } catch (err) {
      setDialogError(err.message)
    }
  }

  function openDialog(kind, appointment) {
    setSelected(appointment)
    setDialogKind(kind)
    setDialogError('')
    setDialogReasonError('')
    if (kind === 'reschedule') {
      setScheduleQuery({ serviceId: appointment.serviceId, date: appointment.date })
      loadServices()
    }
  }

  function closeDialog() {
    setDialogKind(null)
    setSelected(null)
    setScheduleQuery(null)
    setServices([])
    setSlots([])
    setIsSlotsLoading(false)
    setSlotsError('')
    setDialogError('')
    setDialogReasonError('')
    setIsSubmitting(false)
  }

  function handleDone() {
    closeDialog()
    loadAppointments(applied)
  }

  async function handleCancelConfirm({ reason }) {
    if (isSubmitting) return

    setIsSubmitting(true)
    setDialogError('')
    setDialogReasonError('')
    try {
      await cancelAppointment(selected.id, reason)
      handleDone()
    } catch (err) {
      showDialogError(err, 'No fue posible anular la cita.', setDialogError, setDialogReasonError)
      if (STALE_APPOINTMENT_CODES.includes(err.code)) {
        loadAppointments(applied)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleRescheduleConfirm(payload) {
    if (isSubmitting) return

    setIsSubmitting(true)
    setDialogError('')
    setDialogReasonError('')
    try {
      await updateAppointment(selected.id, payload)
      handleDone()
    } catch (err) {
      showDialogError(err, 'No fue posible reprogramar la cita.', setDialogError, setDialogReasonError)
      if (STALE_SLOT_CODES.includes(err.code)) {
        setSlotsVersion((version) => version + 1)
      }
      if (STALE_APPOINTMENT_CODES.includes(err.code)) {
        loadAppointments(applied)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">{isAdmin ? 'Citas' : 'Mis citas'}</h1>

      <AppointmentFilters
        values={draft}
        errors={draftErrors}
        onChange={updateDraft}
        onSearch={handleSearch}
        onClear={handleClear}
        isBusy={isLoading}
      />

      {loadError && <LoadErrorState message={loadError} onRetry={() => loadAppointments(applied)} />}

      {!loadError && !isLoading && appointments.length === 0 && (
        <EmptyState message="No hay citas que coincidan con los filtros." />
      )}

      {!loadError && (isLoading || appointments.length > 0) && (
        <AppointmentsTable
          appointments={appointments}
          isLoading={isLoading}
          isAdmin={isAdmin}
          onReschedule={(appointment) => openDialog('reschedule', appointment)}
          onCancel={(appointment) => openDialog('cancel', appointment)}
        />
      )}

      {isAdmin && dialogKind === 'reschedule' && selected && (
        <RescheduleDialog
          appointment={selected}
          services={services}
          slots={slots}
          isSlotsLoading={isSlotsLoading}
          slotsError={slotsError}
          slotsVersion={slotsVersion}
          isSubmitting={isSubmitting}
          formError={dialogError}
          reasonError={dialogReasonError}
          onScheduleChange={setScheduleQuery}
          onConfirm={handleRescheduleConfirm}
          onClose={closeDialog}
        />
      )}

      {isAdmin && dialogKind === 'cancel' && selected && (
        <CancelDialog
          appointment={selected}
          isSubmitting={isSubmitting}
          formError={dialogError}
          reasonError={dialogReasonError}
          onConfirm={handleCancelConfirm}
          onClose={closeDialog}
        />
      )}
    </section>
  )
}

export default AppointmentsPage
