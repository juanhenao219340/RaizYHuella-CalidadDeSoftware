using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Domain.Entities;
using Veterinaria.Infrastructure.Identity;
using Veterinaria.Infrastructure.Persistence.Models;

namespace Veterinaria.Infrastructure.Persistence;

public sealed class VeterinaryDbContext(DbContextOptions<VeterinaryDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Pet> Pets => Set<Pet>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<ReferenceNumberSequence> ReferenceNumberSequences => Set<ReferenceNumberSequence>();

    public DbSet<SlotControl> SlotControls => Set<SlotControl>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(VeterinaryDbContext).Assembly);
    }
}
