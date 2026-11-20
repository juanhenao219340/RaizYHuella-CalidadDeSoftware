// Pure helper import from Services/ (format.js): no network calls in this component.
import { formatSlotCount } from '../Services/format'

// Time slots from GET /api/appointments/availability. Unavailable slots are disabled (plan D2-A).
// Presentation only: the API decides availability and validates every reservation.
function AvailabilityPicker({ slots = [], value = '', onChange, disabled = false, idPrefix = 'slot' }) {
  return (
    <div role="group" aria-label="Hora de inicio" className="flex flex-col gap-2 md:grid md:grid-cols-5">
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
            className={`min-h-11 flex items-center justify-between gap-2 rounded-md border px-3 py-2 text-left tabular-nums disabled:cursor-not-allowed disabled:opacity-60 ${
              isSelected ? 'border-primary bg-primary-light font-semibold text-primary' : 'border-border bg-surface'
            }`}
          >
            <span className="font-semibold">{slot.startTime}</span>
            <span className="text-sm">{statusText}</span>
          </button>
        )
      })}
    </div>
  )
}

export default AvailabilityPicker
