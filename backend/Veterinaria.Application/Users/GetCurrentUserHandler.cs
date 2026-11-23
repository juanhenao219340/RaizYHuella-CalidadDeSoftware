using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Users;


public sealed class GetCurrentUserHandler(IUserRepository userRepository)
{
    public async Task<User?> HandleAsync(Guid callerId, CancellationToken cancellationToken)
    {
        var current = await userRepository.GetByIdAsync(callerId, cancellationToken);

        return current is { IsDeleted: false } ? current : null;
    }
}
