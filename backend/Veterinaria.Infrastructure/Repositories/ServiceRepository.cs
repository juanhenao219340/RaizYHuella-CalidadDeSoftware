using Microsoft.EntityFrameworkCore;
using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Infrastructure.Persistence;

namespace Veterinaria.Infrastructure.Repositories;

public sealed class ServiceRepository(VeterinaryDbContext context) : IServiceRepository
{
    public async Task<IReadOnlyList<Service>> ListAsync(CancellationToken cancellationToken) =>
        await context.Services
            .AsNoTracking()
            .Where(service => service.DeletedAt == null)
            .OrderBy(service => service.Name)
            .ToListAsync(cancellationToken);

    public Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Services.FirstOrDefaultAsync(service => service.Id == id && service.DeletedAt == null, cancellationToken);

    public Task<Service?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken) =>
        context.Services.AsNoTracking().FirstOrDefaultAsync(service => service.Id == id, cancellationToken);

    public async Task CreateAsync(Service service, CancellationToken cancellationToken)
    {
        context.Services.Add(service);
        await PersistenceErrors.SaveChangesAsync(context, cancellationToken);
    }

    public async Task UpdateAsync(Service service, CancellationToken cancellationToken)
    {
        if (context.Entry(service).State == EntityState.Detached)
        {
            context.Services.Update(service);
        }

        await PersistenceErrors.SaveChangesAsync(context, cancellationToken);
    }
}
