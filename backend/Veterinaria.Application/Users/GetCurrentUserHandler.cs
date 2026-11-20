using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Users;

// Self-service read of the caller's own account (GET /api/users/me). Not subject to the administrator rule (D-2).
public sealed class GetCurrentUserHandler(IUserRepository userRepository)
{
    public async Task<User?> HandleAsync(Guid callerId, CancellationToken cancellationToken)
    {
        var current = await userRepository.GetByIdAsync(callerId, cancellationToken);

        return current is { IsDeleted: false } ? current : null;
    }
}
