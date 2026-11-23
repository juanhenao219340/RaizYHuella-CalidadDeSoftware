import Button from './Button'
import Table from './Table'

import { formatAppointmentStatus, formatDateTime } from '../Services/format'


function AppointmentsTable({ appointments, isLoading = false, isAdmin = false, onReschedule, onCancel }) {
  const columns = [
    { key: 'referenceNumber', label: 'Radicado', render: (item) => item.referenceNumber },
    { key: 'petName', label: 'Mascota', render: (item) => item.petName },

    ...(isAdmin
      ? [{ key: 'ownerName', label: 'Propietario', render: (item) => item.ownerName ?? '—' }]
      : []),
    { key: 'serviceName', label: 'Servicio', render: (item) => item.serviceName },
    {
      key: 'dateTime',
      label: 'Fecha y hora',
      render: (item) => formatDateTime(item.date, item.startTime),
    },
    { key: 'status', label: 'Estado', render: (item) => formatAppointmentStatus(item.status) },
  ]

  if (isAdmin) {
    columns.push({
      key: 'actions',
      label: 'Acciones',
      srOnly: true,
      render: (item) => {
        if (item.status !== 'Confirmed') {
          return <span aria-label="Sin acciones disponibles">—</span>
        }
        return (
          <div className="flex gap-1">
            <Button
              variant="ghost"
              aria-label={`Reprogramar la cita ${item.referenceNumber}`}
              onClick={() => onReschedule(item)}
            >
              <span aria-hidden="true">✎</span>
            </Button>
            <Button
              variant="ghost"
              aria-label={`Anular la cita ${item.referenceNumber}`}
              onClick={() => onCancel(item)}
            >
              <span aria-hidden="true">✕</span>
            </Button>
          </div>
        )
      },
    })
  }

  return (
    <Table
      caption="Citas"
      columns={columns}
      rows={appointments}
      getRowKey={(item) => item.id}
      isLoading={isLoading}
    />
  )
}

export default AppointmentsTable
