using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Users;

public sealed class ListOwnersHandler(IUserRepository userRepository)
{
    public Task<IReadOnlyList<User>> HandleAsync(CancellationToken cancellationToken) =>
        userRepository.ListOwnersAsync(cancellationToken);
}
