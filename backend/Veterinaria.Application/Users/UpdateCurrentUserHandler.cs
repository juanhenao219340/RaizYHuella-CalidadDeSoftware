using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Users;

public sealed record UpdateCurrentUserCommand(Guid CallerId, string Name, string Email, string? Phone);

public sealed class UpdateCurrentUserHandler(IUserRepository userRepository)
{
    public async Task<User?> HandleAsync(UpdateCurrentUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var current = await userRepository.GetByIdAsync(command.CallerId, cancellationToken);

        if (current is null || current.IsDeleted)
        {
            return null;
        }

        return await UserAccessRules.ApplyProfileChangeAsync(
            userRepository, current, command.Name, command.Email, command.Phone, cancellationToken);
    }
}
