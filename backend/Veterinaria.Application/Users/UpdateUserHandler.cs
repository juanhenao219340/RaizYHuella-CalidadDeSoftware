using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;

namespace Veterinaria.Application.Users;

public sealed record UpdateUserCommand(
    Guid TargetUserId,
    Guid CallerId,
    UserRole CallerRole,
    string Name,
    string Email,
    string? Phone);

public sealed class UpdateUserHandler(IUserRepository userRepository)
{
    public async Task<User?> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var target = await userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);

        if (!UserAccessRules.IsVisible(target, command.CallerId, command.CallerRole) || target is null)
        {
            return null;
        }

        return await UserAccessRules.ApplyProfileChangeAsync(
            userRepository, target, command.Name, command.Email, command.Phone, cancellationToken);
    }
}
