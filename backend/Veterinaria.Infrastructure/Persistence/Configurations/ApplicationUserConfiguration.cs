using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veterinaria.Infrastructure.Identity;

namespace Veterinaria.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers");

        builder.Property(user => user.Identification).HasMaxLength(20).IsRequired();
        builder.Property(user => user.Name).HasMaxLength(150).IsRequired();
        builder.Property(user => user.Phone).HasMaxLength(30);

        builder.HasIndex(user => user.Identification)
            .IsUnique()
            .HasDatabaseName("IX_AspNetUsers_Identification");

        // Replaces the non-unique "EmailIndex" created by IdentityDbContext on the same column.
        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("IX_AspNetUsers_NormalizedEmail");
    }
}
