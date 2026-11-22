using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Infrastructure.Configuration;
using Veterinaria.Infrastructure.Identity;
using Veterinaria.Infrastructure.Persistence;

namespace Veterinaria.Infrastructure.Seed;

public static class SeedData
{
    public static readonly Guid ClinicId = new("6f1a0c2e-1b7d-4c8a-9e22-000000000001");
    public static readonly Guid AestheticsId = new("6f1a0c2e-1b7d-4c8a-9e22-000000000002");
    public static readonly Guid RecreationId = new("6f1a0c2e-1b7d-4c8a-9e22-000000000003");

    public static async Task EnsureRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        await EnsureRoleAsync(roleManager, RoleNames.Administrator, RoleNames.AdministratorId);
        await EnsureRoleAsync(roleManager, RoleNames.Owner, RoleNames.OwnerId);
    }

    public static async Task EnsureServicesAsync(VeterinaryDbContext context, CancellationToken cancellationToken)
    {
        var defaults = new (Guid Id, string Name, ServiceType Type)[]
        {
            (ClinicId, "Clínica", ServiceType.Clinic),
            (AestheticsId, "Estética", ServiceType.Aesthetics),
            (RecreationId, "Recreación", ServiceType.Recreation)
        };

        var ids = defaults.Select(item => item.Id).ToArray();

        var existingIds = await context.Services
            .Where(service => ids.Contains(service.Id))
            .Select(service => service.Id)
            .ToListAsync(cancellationToken);

        var missing = defaults.Where(item => !existingIds.Contains(item.Id)).ToList();

        if (missing.Count == 0)
        {
            return;
        }

        foreach (var item in missing)
        {
            context.Services.Add(new Service(item.Id, item.Name, item.Type));
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another start-up created the same fixed-Id rows concurrently; the rows exist, nothing else to do.
        }
    }

    public static async Task EnsureAdministratorAsync(
        VeterinaryDbContext context,
        UserManager<ApplicationUser> userManager,
        SeedOptions seed,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(seed.Email);

        if (existing is not null)
        {
            // Idempotent: an existing administrator is never modified (no password or data changes).
            var roles = await userManager.GetRolesAsync(existing);

            if (!roles.Contains(RoleNames.Administrator))
            {
                logger.LogWarning(
                    "Seed administrator '{Email}' exists without the Administrator role; it was left unchanged.",
                    seed.Email);
            }

            return;
        }

        var administrator = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = seed.Email,
            Email = seed.Email,
            EmailConfirmed = true,
            Identification = seed.Identification,
            Name = seed.Name
        };

        // Creation and role assignment commit together: if the role fails, the account is not kept (same pattern as UserRepository.CreateAsync).
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var created = await userManager.CreateAsync(administrator, seed.Password);

        if (!created.Succeeded)
        {
            if (created.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return;
            }

            throw new InvalidOperationException(
                "The seed administrator could not be created: " + string.Join("; ", created.Errors.Select(error => error.Description)));
        }

        var roleAdded = await userManager.AddToRoleAsync(administrator, RoleNames.Administrator);

        if (!roleAdded.Succeeded)
        {
            throw new InvalidOperationException(
                "The seed administrator could not be assigned the Administrator role; the account was not created: "
                + string.Join("; ", roleAdded.Errors.Select(error => error.Description)));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string name, Guid id)
    {
        if (await roleManager.RoleExistsAsync(name))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole<Guid> { Id = id, Name = name });

        if (!result.Succeeded && !result.Errors.Any(error => error.Code == "DuplicateRoleName"))
        {
            throw new InvalidOperationException(
                $"Role '{name}' could not be created: " + string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
