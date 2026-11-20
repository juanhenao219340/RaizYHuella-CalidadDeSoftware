import { useCallback, useEffect, useState } from 'react'
import Button from '../Components/Button'
import ConfirmDialog from '../Components/ConfirmDialog'
import EmptyState from '../Components/EmptyState'
import LoadErrorState from '../Components/LoadErrorState'
import Modal from '../Components/Modal'
import ServiceForm from '../Components/ServiceForm'
import Table from '../Components/Table'
import { createService, deleteService, listServices, updateService } from '../Services/servicesService'
import { formatServiceType } from '../Services/format'

// Service catalog CRUD (RF-REG-09, CA-REG-05). Deleted services disappear; their future appointments stay.
function ServicesPage() {
  const [services, setServices] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [dialogMode, setDialogMode] = useState(null) // 'create' | 'edit' | null
  const [selectedService, setSelectedService] = useState(null)
  const [serviceToDelete, setServiceToDelete] = useState(null)
  const [deleteError, setDeleteError] = useState('')
  const [isDeleting, setIsDeleting] = useState(false)

  const loadServices = useCallback(async () => {
    setIsLoading(true)
    setLoadError('')
    try {
      setServices(await listServices())
    } catch (err) {
      setLoadError(err.message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    loadServices()
  }, [loadServices])

  function openCreate() {
    setSelectedService(null)
    setDialogMode('create')
  }

  function openEdit(service) {
    setSelectedService(service)
    setDialogMode('edit')
  }

  function closeDialog() {
    setDialogMode(null)
    setSelectedService(null)
  }

  async function handleSave(values) {
    if (dialogMode === 'edit' && selectedService) {
      await updateService(selectedService.id, values)
    } else {
      await createService(values)
    }
    closeDialog()
    await loadServices()
  }

  async function handleDelete() {
    setIsDeleting(true)
    setDeleteError('')
    try {
      await deleteService(serviceToDelete.id)
      setServiceToDelete(null)
      await loadServices()
    } catch (err) {
      setDeleteError(err.message)
    } finally {
      setIsDeleting(false)
    }
  }

  const columns = [
    { key: 'name', label: 'Nombre', render: (service) => service.name },
    { key: 'type', label: 'Tipo', render: (service) => formatServiceType(service.type) },
    {
      key: 'actions',
      label: 'Acciones',
      srOnly: true,
      render: (service) => (
        <div className="flex gap-1">
          <Button variant="ghost" aria-label={`Editar el servicio ${service.name}`} onClick={() => openEdit(service)}>
            <span aria-hidden="true">✎</span>
          </Button>
          <Button
            variant="ghost"
            aria-label={`Eliminar el servicio ${service.name}`}
            onClick={() => {
              setDeleteError('')
              setServiceToDelete(service)
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
        <h1 className="text-2xl font-bold">Servicios</h1>
        <Button onClick={openCreate}>+ Nuevo</Button>
      </header>

      {loadError && <LoadErrorState message={loadError} onRetry={loadServices} />}

      {!loadError && !isLoading && services.length === 0 && (
        <EmptyState message="Todavía no hay servicios registrados.">
          <Button onClick={openCreate}>+ Nuevo</Button>
        </EmptyState>
      )}

      {!loadError && (isLoading || services.length > 0) && (
        <Table
          caption="Servicios"
          columns={columns}
          rows={services}
          getRowKey={(service) => service.id}
          isLoading={isLoading}
        />
      )}

      {dialogMode && (
        <Modal title={dialogMode === 'create' ? 'Nuevo servicio' : 'Editar servicio'} onClose={closeDialog}>
          <ServiceForm
            initialValues={selectedService ?? {}}
            onSubmit={handleSave}
            onCancel={closeDialog}
          />
        </Modal>
      )}

      {serviceToDelete && (
        <ConfirmDialog
          title={`¿Eliminar ${serviceToDelete.name}?`}
          message="El servicio dejará de mostrarse. Sus citas futuras se conservan."
          confirmLabel="Eliminar"
          onConfirm={handleDelete}
          onCancel={() => setServiceToDelete(null)}
          isBusy={isDeleting}
          error={deleteError}
        />
      )}
    </section>
  )
}

export default ServicesPage
