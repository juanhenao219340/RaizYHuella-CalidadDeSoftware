import { useCallback, useEffect, useMemo, useState } from 'react'
import Button from '../Components/Button'
import ConfirmDialog from '../Components/ConfirmDialog'
import EmptyState from '../Components/EmptyState'
import FormField from '../Components/FormField'
import LoadErrorState from '../Components/LoadErrorState'
import Modal from '../Components/Modal'
import OwnerForm from '../Components/OwnerForm'
import Spinner from '../Components/Spinner'
import Table from '../Components/Table'
import { createOwner, deleteUser, getUser, listOwners, updateUser } from '../Services/usersService'


function OwnersPage() {
  const [owners, setOwners] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [query, setQuery] = useState('')
  const [dialogMode, setDialogMode] = useState(null)
  const [selectedOwner, setSelectedOwner] = useState(null)
  const [isLoadingDetail, setIsLoadingDetail] = useState(false)
  const [detailError, setDetailError] = useState('')
  const [ownerToDelete, setOwnerToDelete] = useState(null)
  const [deleteError, setDeleteError] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)

  const loadOwners = useCallback(async () => {
    setIsLoading(true)
    setLoadError('')
    try {
      setOwners(await listOwners())
    } catch (err) {
      setLoadError(err.message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    loadOwners()
  }, [loadOwners])

  const visibleOwners = useMemo(() => {
    const term = query.trim().toLowerCase()
    if (!term) return owners
    return owners.filter((owner) =>
      [owner.name, owner.identification, owner.email].some((value) =>
        (value ?? '').toLowerCase().includes(term),
      ),
    )
  }, [owners, query])

  function openCreate() {
    setSelectedOwner(null)
    setDetailError('')
    setDialogMode('create')
  }


  async function openEdit(owner) {
    setDialogMode('edit')
    setSelectedOwner(owner)
    setDetailError('')
    setIsLoadingDetail(true)
    try {
      setSelectedOwner(await getUser(owner.id))
    } catch (err) {
      setDetailError(err.message)
    } finally {
      setIsLoadingDetail(false)
    }
  }

  function closeDialog() {
    setDialogMode(null)
    setSelectedOwner(null)
  }

  async function handleSave(values) {
    if (dialogMode === 'edit' && selectedOwner) {
      await updateUser(selectedOwner.id, values)
    } else {
      await createOwner(values)
    }
    closeDialog()
    await loadOwners()
  }

  async function handleDelete() {
    setIsDeleting(true)
    setDeleteError('')
    try {
      await deleteUser(ownerToDelete.id)
      setOwnerToDelete(null)
      await loadOwners()
    } catch (err) {
      setDeleteError(err.message)
    } finally {
      setIsDeleting(false)
    }
  }

  const columns = [
    { key: 'identification', label: 'Identificación', render: (owner) => owner.identification },
    { key: 'name', label: 'Nombre', render: (owner) => owner.name },
    { key: 'email', label: 'Correo', render: (owner) => owner.email },
    { key: 'phone', label: 'Teléfono', render: (owner) => owner.phone || '—' },
    {
      key: 'actions',
      label: 'Acciones',
      srOnly: true,
      render: (owner) => (
        <div className="flex gap-1">
          <Button variant="ghost" aria-label={`Editar a ${owner.name}`} onClick={() => openEdit(owner)}>
            <span aria-hidden="true">✎</span>
          </Button>
          <Button
            variant="ghost"
            aria-label={`Eliminar a ${owner.name}`}
            onClick={() => {
              setDeleteError('')
              setOwnerToDelete(owner)
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
        <h1 className="text-2xl font-bold">Propietarios</h1>
        <Button onClick={openCreate}>+ Nuevo</Button>
      </header>

      <div className="max-w-sm">
        <FormField
          id="owners-search"
          label="Buscar"
          type="search"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
      </div>

      {loadError && <LoadErrorState message={loadError} onRetry={loadOwners} />}

      {!loadError && !isLoading && owners.length === 0 && (
        <EmptyState message="Todavía no hay propietarios registrados.">
          <Button onClick={openCreate}>+ Nuevo</Button>
        </EmptyState>
      )}

      {!loadError && !isLoading && owners.length > 0 && visibleOwners.length === 0 && (
        <p>No hay propietarios que coincidan con la búsqueda.</p>
      )}

      {!loadError && (isLoading || visibleOwners.length > 0) && (
        <Table
          caption="Propietarios"
          columns={columns}
          rows={visibleOwners}
          getRowKey={(owner) => owner.id}
          isLoading={isLoading}
        />
      )}

      {dialogMode && (
        <Modal title={dialogMode === 'create' ? 'Nuevo propietario' : 'Editar propietario'} onClose={closeDialog}>
          {isLoadingDetail && <Spinner label="Cargando el propietario…" />}
          {!isLoadingDetail && detailError && <p role="alert" className="text-danger">{detailError}</p>}
          {!isLoadingDetail && !detailError && (
            <OwnerForm
              mode={dialogMode}
              initialValues={selectedOwner ?? {}}
              onSubmit={handleSave}
              onCancel={closeDialog}
            />
          )}
        </Modal>
      )}

      {ownerToDelete && (
        <ConfirmDialog
          title={`¿Eliminar a ${ownerToDelete.name}?`}
          message="El propietario dejará de poder iniciar sesión. Sus mascotas y citas se conservan."
          confirmLabel="Eliminar"
          onConfirm={handleDelete}
          onCancel={() => setOwnerToDelete(null)}
          isBusy={isDeleting}
          error={deleteError}
        />
      )}
    </section>
  )
}

export default OwnersPage
