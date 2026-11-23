import { useState } from 'react'
import Button from './Button'
import FormField from './FormField'

import { SERVICE_TYPE_OPTIONS } from '../Services/format'




function AppointmentFilters({ values, errors = {}, onChange, onSearch, onClear, isBusy = false, isAdmin = false }) {
  const [isExpanded, setIsExpanded] = useState(false)

  return (
    <form
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        onSearch()
      }}
      className="flex flex-col gap-4"
    >
      <Button
        variant="secondary"
        onClick={() => setIsExpanded((current) => !current)}
        aria-expanded={isExpanded}
        aria-controls="appointment-filters-panel"
        className="md:hidden focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      >
        Filtrar
      </Button>

      <div
        id="appointment-filters-panel"
        className={`flex flex-col gap-4 ${isExpanded ? '' : 'max-md:hidden'}`}
      >
        <div className="grid gap-4 md:grid-cols-2">
          <FormField
            id="filter-reference"
            label="Radicado"
            value={values.referenceNumber}
            onChange={(event) => onChange('referenceNumber', event.target.value)}
            maxLength={20}
            error={errors.referenceNumber}
          />
          <FormField
            id="filter-pet-name"
            label="Nombre de la mascota"
            value={values.petName}
            onChange={(event) => onChange('petName', event.target.value)}
            maxLength={100}
            error={errors.petName}
          />
          {isAdmin && (
            <FormField
              id="filter-owner"
              label="Identificación del propietario"
              value={values.ownerIdentification}
              onChange={(event) => onChange('ownerIdentification', event.target.value)}
              maxLength={20}
              error={errors.ownerIdentification}
            />
          )}
          <FormField
            id="filter-service"
            as="select"
            label="Servicio"
            value={values.serviceType}
            onChange={(event) => onChange('serviceType', event.target.value)}
          >
            <option value="">Todos</option>
            {SERVICE_TYPE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </FormField>
        </div>

        <div className="flex justify-end gap-3">
          <Button variant="secondary" onClick={onClear} disabled={isBusy}>
            Limpiar
          </Button>
          <Button type="submit" isLoading={isBusy} disabled={isBusy}>
            Buscar
          </Button>
        </div>
      </div>
    </form>
  )
}

export default AppointmentFilters
