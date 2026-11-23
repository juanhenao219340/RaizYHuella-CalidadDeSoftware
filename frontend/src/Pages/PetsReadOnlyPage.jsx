import { useCallback, useEffect, useMemo, useState } from 'react'
import Button from '../Components/Button'
import EmptyState from '../Components/EmptyState'
import FormField from '../Components/FormField'
import LoadErrorState from '../Components/LoadErrorState'
import Modal from '../Components/Modal'
import Spinner from '../Components/Spinner'
import Table from '../Components/Table'
import { getPet, listPets } from '../Services/petsService'
import { formatDate } from '../Services/format'



function OwnerCell({ pet }) {
  if (pet.ownerName == null) return '—'
  return (
    <span className="flex flex-col items-end gap-0.5 md:items-start">
      <span>{pet.ownerName}</span>
      {pet.ownerDeleted === true && (
        <span className="inline-flex items-center gap-1 text-sm font-semibold text-warning">
          <span aria-hidden="true">⚠</span>
          Propietario eliminado
        </span>
      )}
    </span>
  )
}

function PetsReadOnlyPage() {
  const [pets, setPets] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [query, setQuery] = useState('')
  const [detail, setDetail] = useState(null)
  const [isLoadingDetail, setIsLoadingDetail] = useState(false)
  const [detailError, setDetailError] = useState('')

  const loadPets = useCallback(async () => {
    setIsLoading(true)
    setLoadError('')
    try {
      setPets(await listPets())
    } catch (err) {
      setLoadError(err.message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    loadPets()
  }, [loadPets])

  const visiblePets = useMemo(() => {
    const term = query.trim().toLowerCase()
    if (!term) return pets
    return pets.filter((pet) =>
      [pet.name].some((value) => (value ?? '').toLowerCase().includes(term)),
    )
  }, [pets, query])

  async function openDetail(pet) {
    setDetail(pet)
    setDetailError('')
    setIsLoadingDetail(true)
    try {
      setDetail(await getPet(pet.id))
    } catch (err) {
      setDetailError(err.message)
    } finally {
      setIsLoadingDetail(false)
    }
  }

  const columns = [
    { key: 'name', label: 'Nombre', render: (pet) => pet.name },
    { key: 'owner', label: 'Propietario', render: (pet) => <OwnerCell pet={pet} /> },
    { key: 'species', label: 'Especie', render: (pet) => pet.species },
    { key: 'breed', label: 'Raza', render: (pet) => pet.breed },
    {
      key: 'detail',
      label: 'Detalle',
      srOnly: true,
      render: (pet) => (
        <Button variant="ghost" aria-label={`Ver el detalle de ${pet.name}`} onClick={() => openDetail(pet)}>
          Ver
        </Button>
      ),
    },
  ]

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-2xl font-bold">Mascotas (solo consulta)</h1>

      <div className="max-w-sm">
        <FormField
          id="pets-search"
          label="Buscar"
          type="search"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
      </div>

      {loadError && <LoadErrorState message={loadError} onRetry={loadPets} />}

      {!loadError && !isLoading && pets.length === 0 && (
        <EmptyState message="Todavía no hay mascotas registradas." />
      )}

      {!loadError && !isLoading && pets.length > 0 && visiblePets.length === 0 && (
        <p>No hay mascotas que coincidan con la búsqueda.</p>
      )}

      {!loadError && (isLoading || visiblePets.length > 0) && (
        <Table
          caption="Mascotas, solo consulta"
          columns={columns}
          rows={visiblePets}
          getRowKey={(pet) => pet.id}
          isLoading={isLoading}
        />
      )}

      {detail && (
        <Modal title={`Mascota ${detail.name}`} onClose={() => setDetail(null)}>
          {isLoadingDetail && <Spinner label="Cargando el detalle…" />}
          {!isLoadingDetail && detailError && (
            <p role="alert" className="text-danger">
              {detailError}
            </p>
          )}
          {!isLoadingDetail && !detailError && (
            <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2">
              <dt className="font-semibold">Nombre</dt>
              <dd>{detail.name}</dd>
              <dt className="font-semibold">Especie</dt>
              <dd>{detail.species}</dd>
              <dt className="font-semibold">Raza</dt>
              <dd>{detail.breed}</dd>
              <dt className="font-semibold">Fecha de nacimiento</dt>
              <dd className="tabular-nums">{formatDate(detail.birthDate)}</dd>
            </dl>
          )}
        </Modal>
      )}
    </section>
  )
}

export default PetsReadOnlyPage
