
import { formatSlotCount } from '../Services/format'



function AvailabilityPicker({ slots = [], value = '', onChange, disabled = false, idPrefix = 'slot' }) {
  return (
    <div role="group" aria-label="Hora de inicio" className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-5">
      {slots.map((slot) => {
        const isSelected = value === slot.startTime
        const isSelectable = slot.available && !disabled
        const statusText = slot.available
          ? formatSlotCount(slot.remainingSlots)
          : slot.remainingSlots === 0
            ? 'Sin cupo'
            : 'No disponible'

        return (
          <button
            key={slot.startTime}
            id={`${idPrefix}-${slot.startTime.replace(':', '')}`}
            type="button"
            aria-pressed={isSelected}
            disabled={!isSelectable}
            onClick={() => onChange(slot.startTime)}
            className={`flex min-h-11 min-w-0 flex-col items-center justify-center gap-1 rounded-md border px-2 py-2 text-center break-words tabular-nums disabled:cursor-not-allowed disabled:opacity-60 ${
              isSelected ? 'border-primary bg-primary-light font-semibold text-primary' : 'border-border bg-surface'
            }`}
          >
            <span className="font-semibold">{slot.startTime}</span>
            <span className="whitespace-nowrap text-sm">{statusText}</span>
          </button>
        )
      })}
    </div>
  )
}

export default AvailabilityPicker
