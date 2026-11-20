using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veterinaria.Infrastructure.Configuration;
using Veterinaria.Infrastructure.Identity;
using Veterinaria.Infrastructure.Persistence;

namespace Veterinaria.Infrastructure.Seed;

public sealed class SeedRunner(
    VeterinaryDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    SeedOptions seedOptions,
    ILogger<SeedRunner> logger)
{
    // Called by the host on every start-up: migrates the schema, then seeds idempotently.
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying database migrations.");
        await context.Database.MigrateAsync(cancellationToken);

        foreach (var variable in seedOptions.UndefinedVariables)
        {
            logger.LogWarning("Environment variable {Variable} is not defined; it is mandatory and has no default value.", variable);
        }

        await SeedData.EnsureRolesAsync(roleManager);
        await SeedData.EnsureServicesAsync(context, cancellationToken);
        await SeedData.EnsureAdministratorAsync(context, userManager, seedOptions, logger, cancellationToken);

        logger.LogInformation("Database seed completed.");
    }
}
