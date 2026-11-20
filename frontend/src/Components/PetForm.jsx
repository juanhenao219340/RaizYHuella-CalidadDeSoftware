import { useState } from 'react'
import Button from './Button'
import FormErrorBanner from './FormErrorBanner'
import FormField from './FormField'
// Pure helpers only (no network): see the Components/ import rule in the frontend docs.
import { getConflictField, splitFieldErrors } from '../Services/errorMessages'
import { validatePetCreate, validatePetUpdate } from '../Services/validation'

const FORM_FIELDS = ['name', 'species', 'breed', 'birthDate']

// Pet create/edit form. The parent passes onSubmit; this form does not know the endpoint.
function PetForm({ mode = 'create', initialValues = {}, onSubmit, onCancel }) {
  const isCreate = mode === 'create'
  const [values, setValues] = useState({
    name: initialValues.name ?? '',
    species: initialValues.species ?? '',
    breed: initialValues.breed ?? '',
    birthDate: initialValues.birthDate ?? '',
  })
  const [fieldErrors, setFieldErrors] = useState({})
  const [formError, setFormError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  function updateValue(field, value) {
    setValues((current) => ({ ...current, [field]: value }))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    if (isSubmitting) return

    setFormError('')
    const cleaned = {
      name: values.name.trim(),
      species: values.species.trim(),
      breed: values.breed.trim(),
      birthDate: values.birthDate.trim(),
    }
    const errors = isCreate ? validatePetCreate(cleaned) : validatePetUpdate(cleaned)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    setIsSubmitting(true)
    try {
      await onSubmit(cleaned)
    } catch (err) {
      const conflictField = getConflictField(err)
      if (conflictField) {
        setFieldErrors({ [conflictField]: err.message })
      } else if (Object.keys(err.fieldErrors ?? {}).length > 0) {
        const { fieldErrors: serverErrors, unmatchedMessage } = splitFieldErrors(err.fieldErrors, FORM_FIELDS)
        setFieldErrors(serverErrors)
        setFormError(unmatchedMessage)
      } else {
        setFormError(err.message || 'No fue posible guardar la mascota.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
      <fieldset disabled={isSubmitting} aria-busy={isSubmitting || undefined} className="flex flex-col gap-4">
        <FormField
          id="pet-name"
          label="Nombre"
          value={values.name}
          onChange={(event) => updateValue('name', event.target.value)}
          maxLength={100}
          error={fieldErrors.name}
        />

        <div className="grid gap-4 md:grid-cols-2">
          <FormField
            id="pet-species"
            label="Especie"
            value={values.species}
            onChange={(event) => updateValue('species', event.target.value)}
            maxLength={50}
            error={fieldErrors.species}
          />
          <FormField
            id="pet-breed"
            label="Raza"
            value={values.breed}
            onChange={(event) => updateValue('breed', event.target.value)}
            maxLength={100}
            error={fieldErrors.breed}
          />
        </div>

        <FormField
          id="pet-birth-date"
          label="Fecha de nacimiento"
          type="date"
          value={values.birthDate}
          onChange={(event) => updateValue('birthDate', event.target.value)}
          error={fieldErrors.birthDate}
        />

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

export default PetForm
