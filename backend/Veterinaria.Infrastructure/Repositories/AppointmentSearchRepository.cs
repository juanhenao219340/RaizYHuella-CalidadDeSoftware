using Microsoft.EntityFrameworkCore;
using Veterinaria.Application.Abstractions;
using Veterinaria.Application.Appointments;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.ValueObjects;
using Veterinaria.Infrastructure.Persistence;
using Veterinaria.Infrastructure.Repositories.Search;

namespace Veterinaria.Infrastructure.Repositories;

// One query per search, no per-row queries (T-07 section 3). Filters are combined with AND (successive Where).
// Sin SQL escrito a mano (RNF-ARQ-07).
public sealed class AppointmentSearchRepository(VeterinaryDbContext context) : IAppointmentSearchRepository
{
    public async Task<IReadOnlyList<AppointmentListItem>> SearchAsync(AppointmentSearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var appointments = context.Appointments
            .AsNoTracking()
            .Join(context.Pets, appointment => appointment.PetId, pet => pet.Id, (appointment, pet) => new { Appointment = appointment, Pet = pet })
            .Join(context.Services, row => row.Appointment.ServiceId, service => service.Id, (row, service) => new { row.Appointment, row.Pet, Service = service });

        // Scope first (RF-CON-05). Deleted pets, services and owners are not filtered out (RF-REG-10 f, g).
        if (criteria.OwnerId is { } ownerId)
        {
            appointments = appointments.Where(row => row.Pet.OwnerId == ownerId);
        }

        if (criteria.PetIdentifier is { } petIdentifier)
        {
            var pattern = LikePattern.Contains(petIdentifier);
            appointments = appointments.Where(row => EF.Functions.ILike(row.Pet.Identifier, pattern, LikePattern.EscapeCharacter));
        }

        if (criteria.OwnerIdentification is { } ownerIdentification)
        {
            var pattern = LikePattern.Contains(ownerIdentification);
            appointments = appointments.Where(row => context.Users.Any(user =>
                user.Id == row.Pet.OwnerId && EF.Functions.ILike(user.Identification, pattern, LikePattern.EscapeCharacter)));
        }

        if (criteria.ServiceType is { } serviceType)
        {
            appointments = appointments.Where(row => row.Service.Type == serviceType);
        }

        // Partial, case-insensitive match on the stored text (R13-9, RF-CON-02). Wildcards escaped by LikePattern (RNF-ARQ-07: no manual SQL).
        if (criteria.ReferenceNumber is { } referenceNumber)
        {
            var pattern = LikePattern.Contains(referenceNumber);
            appointments = appointments.Where(row => EF.Functions.ILike(row.Appointment.ReferenceNumber, pattern, LikePattern.EscapeCharacter));
        }

        var rows = await appointments
            .OrderBy(row => row.Appointment.Date)
            .ThenBy(row => row.Appointment.StartTime)
            .ThenBy(row => row.Appointment.ReferenceNumber)
            .Select(row => new AppointmentListRow(
                row.Appointment.Id,
                row.Appointment.ReferenceNumber,
                row.Pet.Id,
                row.Pet.Name,
                row.Service.Id,
                row.Service.Name,
                row.Service.Type,
                row.Appointment.Date,
                row.Appointment.StartTime,
                row.Appointment.Status))
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new AppointmentListItem(
                row.Id,
                row.ReferenceNumber,
                row.PetId,
                row.PetName,
                row.ServiceId,
                row.ServiceName,
                row.ServiceType,
                row.Date,
                row.StartTime.ToString(),
                row.Status))
            .ToList();
    }

    // Flat projection of entity values only. Converted to text in memory, after the single statement.
    private sealed record AppointmentListRow(
        Guid Id,
        string ReferenceNumber,
        Guid PetId,
        string PetName,
        Guid ServiceId,
        string ServiceName,
        ServiceType ServiceType,
        DateOnly Date,
        TimeSlot StartTime,
        AppointmentStatus Status);
}
