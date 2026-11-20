using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Infrastructure.Persistence.Configurations;

public sealed class SlotControlConfiguration : IEntityTypeConfiguration<SlotControl>
{
    public void Configure(EntityTypeBuilder<SlotControl> builder)
    {
        builder.ToTable("SlotControls");

        builder.HasKey(slot => new { slot.Date, slot.StartTime, slot.ServiceId });

        builder.Property(slot => slot.StartTime)
            .HasConversion(slot => slot.StartTime, value => TimeSlot.Create(value))
            .IsRequired();

        builder.Property(slot => slot.Version).IsRequired().HasDefaultValue(0);

        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(slot => slot.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
