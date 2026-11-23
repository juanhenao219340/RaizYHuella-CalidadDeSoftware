import { useCallback, useEffect, useState } from 'react'
import Button from '../Components/Button'
import FormErrorBanner from '../Components/FormErrorBanner'
import FormField from '../Components/FormField'
import LoadErrorState from '../Components/LoadErrorState'
import Spinner from '../Components/Spinner'
import { getConflictField, splitFieldErrors } from '../Services/errorMessages'
import { getMyProfile, updateMyProfile } from '../Services/usersService'
import { validateProfile } from '../Services/validation'

const FORM_FIELDS = ['name', 'email', 'phone']


function ProfilePage() {
  const [profile, setProfile] = useState(null)
  const [values, setValues] = useState({ name: '', email: '', phone: '' })
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [fieldErrors, setFieldErrors] = useState({})
  const [formError, setFormError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  const loadProfile = useCallback(async () => {
    setIsLoading(true)
    setLoadError('')
    try {
      const data = await getMyProfile()
      setProfile(data)
      setValues({ name: data.name ?? '', email: data.email ?? '', phone: data.phone ?? '' })
    } catch (err) {
      setLoadError(err.message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    loadProfile()
  }, [loadProfile])

  function updateValue(field, value) {
    setValues((current) => ({ ...current, [field]: value }))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    if (isSaving) return

    setFormError('')
    setSuccessMessage('')
    const cleaned = {
      name: values.name.trim(),
      email: values.email.trim(),
      phone: values.phone.trim(),
    }
    const errors = validateProfile(cleaned)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    setIsSaving(true)
    try {
      const updated = await updateMyProfile(cleaned)
      setProfile(updated)
      setValues({ name: updated.name ?? '', email: updated.email ?? '', phone: updated.phone ?? '' })
      setSuccessMessage('Cambios guardados.')
    } catch (err) {
      const conflictField = getConflictField(err)
      if (conflictField) {
        setFieldErrors({ [conflictField]: err.message })
      } else if (Object.keys(err.fieldErrors ?? {}).length > 0) {
        const { fieldErrors: serverErrors, unmatchedMessage } = splitFieldErrors(err.fieldErrors, FORM_FIELDS)
        setFieldErrors(serverErrors)
        setFormError(unmatchedMessage)
      } else {
        setFormError(err.message || 'No fue posible guardar los cambios.')
      }
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) return <Spinner label="Cargando el perfil…" />
  if (loadError) return <LoadErrorState message={loadError} onRetry={loadProfile} />

  return (
    <section className="flex max-w-xl flex-col gap-4">
      <h1 className="text-2xl font-bold">Mi perfil</h1>

      <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2">
        <dt className="font-semibold">Identificación</dt>
        <dd className="tabular-nums">{profile?.identification}</dd>
      </dl>

      <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
        <fieldset disabled={isSaving} aria-busy={isSaving || undefined} className="flex flex-col gap-4">
          <FormField
            id="profile-name"
            label="Nombre"
            value={values.name}
            onChange={(event) => updateValue('name', event.target.value)}
            maxLength={150}
            error={fieldErrors.name}
          />
          <FormField
            id="profile-email"
            label="Correo electrónico"
            type="email"
            value={values.email}
            onChange={(event) => updateValue('email', event.target.value)}
            maxLength={256}
            error={fieldErrors.email}
          />
          <FormField
            id="profile-phone"
            label="Teléfono (opcional)"
            value={values.phone}
            onChange={(event) => updateValue('phone', event.target.value)}
            maxLength={30}
            error={fieldErrors.phone}
          />

          <FormErrorBanner message={formError} />
          {successMessage && (
            <p role="status" className="font-semibold text-success">
              {successMessage}
            </p>
          )}

          <div className="flex justify-end">
            <Button type="submit" isLoading={isSaving}>
              Guardar cambios
            </Button>
          </div>
        </fieldset>
      </form>
    </section>
  )
}

export default ProfilePage
