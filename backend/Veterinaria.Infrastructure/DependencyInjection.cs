using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Veterinaria.Application.Abstractions;
using Veterinaria.Application.Authentication;
using Veterinaria.Domain.Configuration;
using Veterinaria.Infrastructure.Configuration;
using Veterinaria.Infrastructure.Identity;
using Veterinaria.Infrastructure.Persistence;
using Veterinaria.Infrastructure.Repositories.Concurrency;
using Veterinaria.Infrastructure.Repositories;
using Veterinaria.Infrastructure.Seed;
using DomainLockoutOptions = Veterinaria.Domain.Configuration.LockoutOptions;

namespace Veterinaria.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection("ConnectionStrings"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Veterinary), "ConnectionStrings:Veterinary is required.")
            .ValidateOnStart();

        services.AddOptions<BusinessRulesOptions>()
            .Bind(configuration.GetSection("BusinessRules"))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<BusinessRulesOptions>, BusinessRulesOptionsValidator>();

        services.AddOptions<DomainLockoutOptions>()
            .Bind(configuration.GetSection("Lockout"))
            .Validate(options => options.MaxFailedAttempts > 0, "Lockout:MaxFailedAttempts must be greater than zero.")
            .Validate(options => options.LockoutMinutes > 0, "Lockout:LockoutMinutes must be greater than zero.")
            .ValidateOnStart();

        // SeedOptions is not an IOptions<T> (its SEED_ADMIN_* keys are flat variables). It is loaded and validated
        // eagerly here, so an invalid value stops composition (and therefore start-up) before anything is served.
        var seedOptions = SeedOptions.Load(configuration);
        SeedOptions.Validate(seedOptions);
        services.AddSingleton(seedOptions);

        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<VeterinaryDbContext>((serviceProvider, options) =>
            options.UseNpgsql(serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value.Veterinary));

        services.AddOptions<IdentityOptions>()
            .Configure<IOptions<DomainLockoutOptions>>((identity, lockout) => IdentityConfiguration.Apply(identity, lockout.Value));

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<VeterinaryDbContext>()
            .AddSignInManager();

        services.AddScoped<IAuthenticationGateway, IdentityAuthenticationGateway>();
        services.AddScoped<IUserAccessGate, UserAccessGate>();
        services.AddScoped<LoginHandler>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IAppointmentSearchRepository, AppointmentSearchRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<ReferenceNumberSequenceCounter>();
        services.AddScoped<SeedRunner>();

        return services;
    }
}
