import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import AvailabilityPicker from '../Components/AvailabilityPicker'
import Button from '../Components/Button'
import EmptyState from '../Components/EmptyState'
import FormErrorBanner from '../Components/FormErrorBanner'
import LoadErrorState from '../Components/LoadErrorState'
import Spinner from '../Components/Spinner'
import { createAppointment, getAvailability } from '../Services/appointmentsService'
import { listPets } from '../Services/petsService'
import { listServices } from '../Services/servicesService'
import { formatServiceType, todayIsoDate } from '../Services/format'
import { validateBooking } from '../Services/validation'
import { LIST_LOAD_ERROR_MESSAGE } from '../Services/errorMessages'


const RELOAD_SLOTS_CODES = ['RN-03', 'P-01', 'RN-01']


function BookAppointmentPage() {
  const [pets, setPets] = useState([])
  const [services, setServices] = useState([])
  const [isCatalogLoading, setIsCatalogLoading] = useState(true)
  const [catalogError, setCatalogError] = useState('')
  const [petId, setPetId] = useState('')
  const [serviceId, setServiceId] = useState('')
  const [date, setDate] = useState('')
  const [startTime, setStartTime] = useState('')
  const [slots, setSlots] = useState([])
  const [isSlotsLoading, setIsSlotsLoading] = useState(false)
  const [slotsError, setSlotsError] = useState('')
  const [slotsRefreshKey, setSlotsRefreshKey] = useState(0)
  const [fieldErrors, setFieldErrors] = useState({})
  const [formError, setFormError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const today = todayIsoDate()

  const loadCatalog = useCallback(async () => {
    setIsCatalogLoading(true)
    setCatalogError('')
    try {
      const [petList, serviceList] = await Promise.all([listPets(), listServices()])
      setPets(petList)
      setServices(serviceList)
    } catch (err) {
      setCatalogError(err.message || LIST_LOAD_ERROR_MESSAGE)
    } finally {
      setIsCatalogLoading(false)
    }
  }, [])

  useEffect(() => {
    loadCatalog()
  }, [loadCatalog])


  useEffect(() => {
    if (!serviceId || !date) {
      setSlots([])
      return undefined
    }

    const controller = new AbortController()
    setIsSlotsLoading(true)
    setSlotsError('')
    getAvailability(serviceId, date, { signal: controller.signal })
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
  }, [serviceId, date, slotsRefreshKey])

  function handleServiceOrDateChange(setter, value) {
    setter(value)
    setStartTime('')
    setSuccessMessage('')
  }

  async function handleSubmit(event) {
    event.preventDefault()
    if (isSubmitting) return

    setFormError('')
    setSuccessMessage('')
    const errors = validateBooking({ petId, serviceId, date, startTime })
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    setIsSubmitting(true)
    try {
      const appointment = await createAppointment({ petId, serviceId, date, startTime })
      setSuccessMessage(`Cita registrada. Radicado ${appointment.referenceNumber}.`)
      setStartTime('')
      setSlotsRefreshKey((key) => key + 1)
    } catch (err) {
      setFormError(err.message || 'No fue posible registrar la cita.')
      if (RELOAD_SLOTS_CODES.includes(err.code)) {
        setStartTime('')
        setSlotsRefreshKey((key) => key + 1)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isCatalogLoading) return <Spinner label="Cargando las mascotas y los servicios…" />
  if (catalogError) return <LoadErrorState message={catalogError} onRetry={loadCatalog} />

  if (pets.length === 0) {
    return (
      <section className="flex flex-col gap-4">
        <h1 className="text-2xl font-bold">Agendar cita</h1>
        <EmptyState message="Todavía no tiene mascotas registradas. Regístrela para agendar una cita.">
          <Link to="/propietario/mascotas" className="font-semibold text-primary underline underline-offset-4">
            Ir a mis mascotas
          </Link>
        </EmptyState>
      </section>
    )
  }

  return (
    <section className="flex max-w-3xl flex-col gap-6">
      <h1 className="text-2xl font-bold">Agendar cita</h1>

      {successMessage && (
        <p role="status" className="rounded-md border border-success bg-surface-muted p-3 font-semibold text-success">
          {successMessage}
        </p>
      )}

      <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-5">
        <fieldset disabled={isSubmitting} aria-busy={isSubmitting || undefined} className="flex flex-col gap-5">
          <div className="flex flex-col gap-1">
            <label htmlFor="booking-pet" className="font-semibold">
              1. Mascota
            </label>
            <select
              id="booking-pet"
              value={petId}
              onChange={(event) => setPetId(event.target.value)}
              aria-invalid={fieldErrors.petId ? true : undefined}
              aria-describedby={fieldErrors.petId ? 'booking-pet-error' : undefined}
              className={`min-h-11 rounded-md border bg-surface px-3 ${fieldErrors.petId ? 'border-danger' : 'border-border'}`}
            >
              <option value="">Seleccione una mascota</option>
              {pets.map((pet) => (
                <option key={pet.id} value={pet.id}>
                  {pet.name}
                </option>
              ))}
            </select>
            {fieldErrors.petId && (
              <p id="booking-pet-error" className="text-sm font-semibold text-danger">
                {fieldErrors.petId}
              </p>
            )}
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="booking-service" className="font-semibold">
              2. Servicio
            </label>
            <select
              id="booking-service"
              value={serviceId}
              onChange={(event) => handleServiceOrDateChange(setServiceId, event.target.value)}
              aria-invalid={fieldErrors.serviceId ? true : undefined}
              aria-describedby={fieldErrors.serviceId ? 'booking-service-error' : undefined}
              className={`min-h-11 rounded-md border bg-surface px-3 ${fieldErrors.serviceId ? 'border-danger' : 'border-border'}`}
            >
              <option value="">Seleccione un servicio</option>
              {services.map((service) => (
                <option key={service.id} value={service.id}>
                  {service.name} ({formatServiceType(service.type)})
                </option>
              ))}
            </select>
            {fieldErrors.serviceId && (
              <p id="booking-service-error" className="text-sm font-semibold text-danger">
                {fieldErrors.serviceId}
              </p>
            )}
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="booking-date" className="font-semibold">
              3. Fecha
            </label>
            <input
              id="booking-date"
              type="date"
              min={today}
              value={date}
              onChange={(event) => handleServiceOrDateChange(setDate, event.target.value)}
              aria-invalid={fieldErrors.date ? true : undefined}
              aria-describedby={fieldErrors.date ? 'booking-date-error' : undefined}
              className={`min-h-11 rounded-md border bg-surface px-3 tabular-nums ${fieldErrors.date ? 'border-danger' : 'border-border'}`}
            />
            {fieldErrors.date && (
              <p id="booking-date-error" className="text-sm font-semibold text-danger">
                {fieldErrors.date}
              </p>
            )}
          </div>

          <div className="flex flex-col gap-2">
            <span className="font-semibold">4. Hora de inicio</span>
            {!serviceId || !date ? (
              <p>Elija un servicio y una fecha para ver las franjas.</p>
            ) : isSlotsLoading ? (
              <Spinner label="Cargando las franjas…" />
            ) : slotsError ? (
              <FormErrorBanner message={slotsError} />
            ) : slots.length === 0 ? (
              <p>No hay cupos para esta fecha. Pruebe otra fecha.</p>
            ) : (
              <AvailabilityPicker
                slots={slots}
                value={startTime}
                onChange={(value) => {
                  setStartTime(value)
                  setFieldErrors((current) => ({ ...current, startTime: undefined }))
                }}
                idPrefix="booking-slot"
              />
            )}
            {fieldErrors.startTime && (
              <p className="text-sm font-semibold text-danger">{fieldErrors.startTime}</p>
            )}
          </div>

          <FormErrorBanner message={formError} />

          <div className="flex justify-end">
            <Button type="submit" isLoading={isSubmitting}>
              Confirmar cita
            </Button>
          </div>
        </fieldset>
      </form>
    </section>
  )
}

export default BookAppointmentPage
