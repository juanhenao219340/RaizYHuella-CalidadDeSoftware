import { useState } from 'react'
import Button from './Button'
import FormErrorBanner from './FormErrorBanner'
import FormField from './FormField'
// Pure helpers only (no network): see the Components/ import rule in the frontend docs.
import { getConflictField, splitFieldErrors } from '../Services/errorMessages'
import { validateOwnerCreate, validateOwnerUpdate } from '../Services/validation'

const FORM_FIELDS = ['identification', 'name', 'email', 'phone', 'password']

// Owner create/edit form. In edit mode identification is read-only and there is no password field (plan 6.2 A1).
function OwnerForm({ mode = 'create', initialValues = {}, onSubmit, onCancel }) {
  const isCreate = mode === 'create'
  const [values, setValues] = useState({
    identification: initialValues.identification ?? '',
    name: initialValues.name ?? '',
    email: initialValues.email ?? '',
    phone: initialValues.phone ?? '',
    password: '',
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
      identification: values.identification.trim(),
      name: values.name.trim(),
      email: values.email.trim(),
      phone: values.phone.trim(),
      password: values.password,
    }
    const errors = isCreate ? validateOwnerCreate(cleaned) : validateOwnerUpdate(cleaned)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    setIsSubmitting(true)
    try {
      await onSubmit(isCreate ? cleaned : { name: cleaned.name, email: cleaned.email, phone: cleaned.phone })
    } catch (err) {
      const conflictField = getConflictField(err)
      if (conflictField) {
        setFieldErrors({ [conflictField]: err.message })
      } else if (Object.keys(err.fieldErrors ?? {}).length > 0) {
        const { fieldErrors: serverErrors, unmatchedMessage } = splitFieldErrors(err.fieldErrors, FORM_FIELDS)
        setFieldErrors(serverErrors)
        setFormError(unmatchedMessage)
      } else {
        setFormError(err.message || 'No fue posible guardar el propietario.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
      <fieldset disabled={isSubmitting} aria-busy={isSubmitting || undefined} className="flex flex-col gap-4">
        {isCreate ? (
          <FormField
            id="owner-identification"
            label="Identificación"
            value={values.identification}
            onChange={(event) => updateValue('identification', event.target.value)}
            maxLength={20}
            error={fieldErrors.identification}
          />
        ) : (
          <div className="flex flex-col gap-1">
            <span className="font-semibold">Identificación</span>
            <p className="tabular-nums">{initialValues.identification}</p>
          </div>
        )}

        <FormField
          id="owner-name"
          label="Nombre"
          value={values.name}
          onChange={(event) => updateValue('name', event.target.value)}
          maxLength={150}
          error={fieldErrors.name}
        />
        <FormField
          id="owner-email"
          label="Correo electrónico"
          type="email"
          value={values.email}
          onChange={(event) => updateValue('email', event.target.value)}
          maxLength={256}
          error={fieldErrors.email}
        />
        <FormField
          id="owner-phone"
          label="Teléfono (opcional)"
          value={values.phone}
          onChange={(event) => updateValue('phone', event.target.value)}
          maxLength={30}
          error={fieldErrors.phone}
        />

        {isCreate && (
          <FormField
            id="owner-password"
            label="Contraseña inicial"
            type="password"
            autoComplete="new-password"
            hint="Mínimo 8 caracteres, con mayúscula, minúscula y dígito."
            value={values.password}
            onChange={(event) => updateValue('password', event.target.value)}
            error={fieldErrors.password}
          />
        )}

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

export default OwnerForm
