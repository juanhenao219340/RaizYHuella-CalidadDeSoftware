namespace Veterinaria.Application.Abstractions;

// Checks, on every protected request, that the token subject still denotes a non-deleted account (RF-AUT-07).
public interface IUserAccessGate
{
    Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken);
}
