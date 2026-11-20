using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Application.Appointments;

public sealed record CreateAppointmentCommand(Guid OwnerId, Guid PetId, Guid ServiceId, DateOnly Date, string StartTime);

// Null means "not found for this caller" (404). Pre-checks give specific messages; the authoritative check is inside the transaction.
public sealed class CreateAppointmentHandler(
    IPetRepository petRepository,
    IServiceRepository serviceRepository,
    IScheduleRepository scheduleRepository,
    BusinessRulesOptions options,
    TimeProvider timeProvider)
{
    public async Task<Appointment?> HandleAsync(CreateAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pet = await petRepository.GetByIdIncludingDeletedAsync(command.PetId, cancellationToken);

        if (pet is null || pet.OwnerId != command.OwnerId)
        {
            return null;
        }

        if (pet.IsDeleted)
        {
            throw new DomainRuleException("RF-REG-10", "La mascota está eliminada y no puede usarse para nuevas citas.");
        }

        var service = await serviceRepository.GetByIdIncludingDeletedAsync(command.ServiceId, cancellationToken);

        if (service is null)
        {
            return null;
        }

        if (service.IsDeleted)
        {
            throw new DomainRuleException("RF-REG-10", "El servicio está eliminado y no puede usarse para nuevas citas.");
        }

        var startTime = AppointmentStartTime.Parse(command.StartTime);
        var request = new AppointmentRequest(pet.Id, command.Date, startTime, service.Id);

        return await scheduleRepository.BookAsync(request, command.OwnerId, timeProvider.GetUtcNow(), options, cancellationToken);
    }
}
