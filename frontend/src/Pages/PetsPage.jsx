import { useCallback, useEffect, useState } from 'react'
import Button from '../Components/Button'
import ConfirmDialog from '../Components/ConfirmDialog'
import EmptyState from '../Components/EmptyState'
import LoadErrorState from '../Components/LoadErrorState'
import Modal from '../Components/Modal'
import PetForm from '../Components/PetForm'
import Table from '../Components/Table'
import { createPet, deletePet, listPets, updatePet } from '../Services/petsService'
import { formatDate } from '../Services/format'

// Owner's pets (RF-REG-02). The API returns only the pets of the signed-in owner.
function PetsPage() {
  const [pets, setPets] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [dialogMode, setDialogMode] = useState(null) // 'create' | 'edit' | null
  const [selectedPet, setSelectedPet] = useState(null)
  const [petToDelete, setPetToDelete] = useState(null)
  const [deleteError, setDeleteError] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)

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

  function openCreate() {
    setSelectedPet(null)
    setDialogMode('create')
  }

  function openEdit(pet) {
    setSelectedPet(pet)
    setDialogMode('edit')
  }

  function closeDialog() {
    setDialogMode(null)
    setSelectedPet(null)
  }

  async function handleSave(values) {
    if (dialogMode === 'edit' && selectedPet) {
      await updatePet(selectedPet.id, values)
    } else {
      await createPet(values)
    }
    closeDialog()
    await loadPets()
  }

  async function handleDelete() {
    setIsDeleting(true)
    setDeleteError('')
    try {
      await deletePet(petToDelete.id)
      setPetToDelete(null)
      await loadPets()
    } catch (err) {
      setDeleteError(err.message)
    } finally {
      setIsDeleting(false)
    }
  }

  const columns = [
    { key: 'identifier', label: 'Identificador', render: (pet) => pet.identifier },
    { key: 'name', label: 'Nombre', render: (pet) => pet.name },
    { key: 'species', label: 'Especie', render: (pet) => pet.species },
    { key: 'breed', label: 'Raza', render: (pet) => pet.breed },
    { key: 'birthDate', label: 'Nacimiento', render: (pet) => formatDate(pet.birthDate) },
    {
      key: 'actions',
      label: 'Acciones',
      srOnly: true,
      render: (pet) => (
        <div className="flex gap-1">
          <Button variant="ghost" aria-label={`Editar la mascota ${pet.name}`} onClick={() => openEdit(pet)}>
            <span aria-hidden="true">✎</span>
          </Button>
          <Button
            variant="ghost"
            aria-label={`Eliminar la mascota ${pet.name}`}
            onClick={() => {
              setDeleteError('')
              setPetToDelete(pet)
            }}
          >
            <span aria-hidden="true">🗑</span>
          </Button>
        </div>
      ),
    },
  ]

  return (
    <section className="flex flex-col gap-4">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Mis mascotas</h1>
        {!isLoading && !loadError && pets.length > 0 && (
          <Button onClick={openCreate}>+ Nueva</Button>
        )}
      </header>

      {loadError && <LoadErrorState message={loadError} onRetry={loadPets} />}

      {!loadError && !isLoading && pets.length === 0 && (
        <EmptyState message="Todavía no tiene mascotas registradas.">
          <Button onClick={openCreate}>+ Registrar mi primera mascota</Button>
        </EmptyState>
      )}

      {!loadError && (isLoading || pets.length > 0) && (
        <Table
          caption="Mis mascotas"
          columns={columns}
          rows={pets}
          getRowKey={(pet) => pet.id}
          isLoading={isLoading}
        />
      )}

      {dialogMode && (
        <Modal title={dialogMode === 'create' ? 'Nueva mascota' : 'Editar mascota'} onClose={closeDialog}>
          <PetForm
            mode={dialogMode}
            initialValues={selectedPet ?? {}}
            onSubmit={handleSave}
            onCancel={closeDialog}
          />
        </Modal>
      )}

      {petToDelete && (
        <ConfirmDialog
          title={`¿Eliminar a ${petToDelete.name}?`}
          message="La mascota dejará de aparecer en su lista. Esta acción no se puede deshacer."
          confirmLabel="Eliminar"
          onConfirm={handleDelete}
          onCancel={() => setPetToDelete(null)}
          isBusy={isDeleting}
          error={deleteError}
        />
      )}
    </section>
  )
}

export default PetsPage
