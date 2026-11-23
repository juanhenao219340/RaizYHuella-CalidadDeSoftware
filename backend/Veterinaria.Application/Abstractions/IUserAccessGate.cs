namespace Veterinaria.Application.Abstractions;


public interface IUserAccessGate
{
    Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken);
}
