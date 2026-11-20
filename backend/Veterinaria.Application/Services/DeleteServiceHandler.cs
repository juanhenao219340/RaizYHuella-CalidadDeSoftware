using Veterinaria.Application.Abstractions;

namespace Veterinaria.Application.Services;

public sealed class DeleteServiceHandler(IServiceRepository serviceRepository, TimeProvider timeProvider)
{
    // Returns false when the service is missing or already deleted (the API answers 404).
    public async Task<bool> HandleAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);

        if (service is null)
        {
            return false;
        }

        service.Delete(timeProvider.GetUtcNow());
        await serviceRepository.UpdateAsync(service, cancellationToken);

        return true;
    }
}
