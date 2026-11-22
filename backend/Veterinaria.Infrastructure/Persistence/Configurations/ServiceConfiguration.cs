using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Infrastructure.Persistence.Configurations;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services", table =>
            table.HasCheckConstraint("CK_Services_Type", @"""Type"" IN ('Clinic', 'Aesthetics', 'Recreation')"));

        builder.Property(service => service.Name).HasMaxLength(100).IsRequired();
        builder.Property(service => service.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
    }
}
