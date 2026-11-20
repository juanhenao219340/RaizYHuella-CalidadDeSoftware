using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Veterinaria.Infrastructure.Persistence.DesignTime;

public sealed class VeterinaryDbContextFactory : IDesignTimeDbContextFactory<VeterinaryDbContext>
{
    private const string FallbackConnectionString = "Host=localhost;Database=veterinary";

    public VeterinaryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Veterinary") ?? FallbackConnectionString;

        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new VeterinaryDbContext(options);
    }
}
