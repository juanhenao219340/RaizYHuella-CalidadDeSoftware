import { useState } from 'react'
import Button from './Button'
import FormErrorBanner from './FormErrorBanner'
import FormField from './FormField'
// Pure helpers only (no network): see the Components/ import rule in the frontend docs.
import { splitFieldErrors } from '../Services/errorMessages'
import { SERVICE_TYPE_OPTIONS } from '../Services/format'
import { validateService } from '../Services/validation'

const FORM_FIELDS = ['name', 'type']

// Service create/edit form. The type is chosen only from the three closed options.
function ServiceForm({ initialValues = {}, onSubmit, onCancel }) {
  const [name, setName] = useState(initialValues.name ?? '')
  const [type, setType] = useState(initialValues.type ?? '')
  const [fieldErrors, setFieldErrors] = useState({})
  const [formError, setFormError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()
    if (isSubmitting) return

    setFormError('')
    const cleaned = { name: name.trim(), type }
    const errors = validateService(cleaned)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    setIsSubmitting(true)
    try {
      await onSubmit(cleaned)
    } catch (err) {
      if (Object.keys(err.fieldErrors ?? {}).length > 0) {
        const { fieldErrors: serverErrors, unmatchedMessage } = splitFieldErrors(err.fieldErrors, FORM_FIELDS)
        setFieldErrors(serverErrors)
        setFormError(unmatchedMessage)
      } else {
        setFormError(err.message || 'No fue posible guardar el servicio.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
      <fieldset disabled={isSubmitting} aria-busy={isSubmitting || undefined} className="flex flex-col gap-4">
        <FormField
          id="service-name"
          label="Nombre"
          value={name}
          onChange={(event) => setName(event.target.value)}
          maxLength={100}
          error={fieldErrors.name}
        />

        <FormField
          id="service-type"
          as="select"
          label="Tipo"
          value={type}
          onChange={(event) => setType(event.target.value)}
          error={fieldErrors.type}
        >
          <option value="">Seleccione un tipo</option>
          {SERVICE_TYPE_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </FormField>

        <FormErrorBanner message={formError} />

        <div className="flex justify-end gap-3">
          <Button variant="secondary" onClick={onCancel}>
            Cancelar
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            Guardar
          </Button>
        </div>
      </fieldset>
    </form>
  )
}

export default ServiceForm
