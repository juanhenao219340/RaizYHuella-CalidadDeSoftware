using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Users;

public sealed record GetUserQuery(Guid TargetUserId, Guid CallerId, UserRole CallerRole);

public sealed class GetUserHandler(IUserRepository userRepository)
{
    public async Task<User?> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var target = await userRepository.GetByIdAsync(query.TargetUserId, cancellationToken);

        return UserAccessRules.IsVisible(target, query.CallerId, query.CallerRole) ? target : null;
    }
}
