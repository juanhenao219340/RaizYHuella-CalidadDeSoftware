using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", table =>
        {
            table.HasCheckConstraint("CK_Appointments_Status", @"""Status"" IN ('Confirmed', 'Cancelled')");
            table.HasCheckConstraint(
                "CK_Appointments_StartTime",
                @"""StartTime"" BETWEEN '08:00' AND '17:00' AND EXTRACT(MINUTE FROM ""StartTime"") = 0 AND EXTRACT(SECOND FROM ""StartTime"") = 0");
        });

        builder.Property(appointment => appointment.ReferenceNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(appointment => appointment.StartTime)
            .HasConversion(slot => slot.StartTime, value => TimeSlot.Create(value))
            .IsRequired();

        builder.Property(appointment => appointment.Status).HasConversion<string>().HasMaxLength(20).IsRequired().IsConcurrencyToken();
        builder.Property(appointment => appointment.ChangeReason).HasMaxLength(500);
        builder.Property(appointment => appointment.CreatedAt).IsRequired();

        // Version is the concurrency token for reschedule and cancel (R13-7).
        builder.Property(appointment => appointment.Version).IsConcurrencyToken();

        builder.HasIndex(appointment => appointment.ReferenceNumber)
            .IsUnique()
            .HasDatabaseName("IX_Appointments_ReferenceNumber");

        builder.HasIndex(appointment => new { appointment.PetId, appointment.Date, appointment.StartTime, appointment.ServiceId })
            .IsUnique()
            .HasFilter(@"""Status"" = 'Confirmed'")
            .HasDatabaseName("IX_Appointments_Pet_Slot_Confirmed");

        builder.HasIndex(appointment => new { appointment.Date, appointment.StartTime, appointment.ServiceId })
            .IncludeProperties(appointment => appointment.Status)
            .HasDatabaseName("IX_Appointments_Slot_Confirmed");

        builder.HasOne<Pet>()
            .WithMany()
            .HasForeignKey(appointment => appointment.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(appointment => appointment.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
