using Microsoft.EntityFrameworkCore;
using Npgsql;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Infrastructure.Persistence;

public static class PersistenceErrors
{
    public const string ConcurrencyCode = "RF-PRO-11";
    public const string ConcurrencyMessage = "La información fue modificada por otra operación. Recárguela e intente de nuevo.";
    public const string AppointmentModifiedMessage = "La cita fue modificada por otra operación; vuelva a consultarla.";
    public const string AppointmentCancelledCode = "RF-PRO-07";
    public const string AppointmentCancelledMessage = AppointmentModificationRules.CancelledMessage;

    public static async Task SaveChangesAsync(VeterinaryDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception) when (exception.Entries.Any(entry => entry.Entity is Appointment))
        {
            throw await TranslateAppointmentConflictAsync(context, exception, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainRuleException(ConcurrencyCode, ConcurrencyMessage);
        }
        catch (DbUpdateException exception) when (Translate(exception) is { } translated)
        {
            throw translated;
        }
    }

    // RF-PRO-07 only when the appointment is cancelled in the database. Any other conflict is a version conflict (RF-PRO-11, 409).
    // A missing row cannot occur today (appointments are never hard-deleted), so it is reported as a version conflict too.
    private static async Task<DomainRuleException> TranslateAppointmentConflictAsync(
        VeterinaryDbContext context,
        DbUpdateConcurrencyException exception,
        CancellationToken cancellationToken)
    {
        var appointmentId = exception.Entries
            .Select(entry => entry.Entity)
            .OfType<Appointment>()
            .First()
            .Id;

        var status = await context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.Id == appointmentId)
            .Select(appointment => (AppointmentStatus?)appointment.Status)
            .FirstOrDefaultAsync(cancellationToken);

        return status == AppointmentStatus.Cancelled
            ? new DomainRuleException(AppointmentCancelledCode, AppointmentCancelledMessage)
            : new DomainRuleException(ConcurrencyCode, AppointmentModifiedMessage);
    }

    public static DomainRuleException? Translate(Exception exception)
    {
        var postgres = FindPostgresException(exception);

        if (postgres is null)
        {
            return null;
        }

        if (postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return postgres.ConstraintName switch
            {
                "IX_AspNetUsers_Identification" => new DomainRuleException("RF-REG-06", "La identificación ya está registrada."),
                "IX_AspNetUsers_NormalizedEmail" => new DomainRuleException("RF-REG-06", "El correo ya está registrado."),
                "IX_Pets_Identifier" => new DomainRuleException("RF-REG-06", "El identificador de la mascota ya está registrado."),
                "IX_Appointments_ReferenceNumber" => new DomainRuleException("RF-PRO-03", "El número de radicado ya existe."),
                "IX_Appointments_Pet_Slot_Confirmed" => new DomainRuleException(
                    "P-02",
                    "La mascota ya tiene una cita activa en esa fecha, hora y servicio."),
                _ => null
            };
        }

        if (postgres.SqlState == PostgresErrorCodes.CheckViolation
            && postgres.ConstraintName == "CK_ReferenceNumberSequences_LastNumber")
        {
            return new DomainRuleException("RF-PRO-03", "Se alcanzó el máximo de 9999 radicados para la fecha.");
        }

        return null;
    }

    private static PostgresException? FindPostgresException(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }
}
