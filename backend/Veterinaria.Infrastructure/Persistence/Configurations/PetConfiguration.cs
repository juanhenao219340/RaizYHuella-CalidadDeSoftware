using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veterinaria.Domain.Entities;
using Veterinaria.Infrastructure.Identity;

namespace Veterinaria.Infrastructure.Persistence.Configurations;

public sealed class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("Pets");

        builder.Property(pet => pet.Name).HasMaxLength(100);
        builder.Property(pet => pet.Species).HasMaxLength(50);
        builder.Property(pet => pet.Breed).HasMaxLength(100);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(pet => pet.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
