using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Application.Users;

public sealed record DeleteUserCommand(Guid TargetUserId, Guid CallerId, UserRole CallerRole);

public sealed class DeleteUserHandler(IUserRepository userRepository, TimeProvider timeProvider)
{

    public async Task<bool> HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var target = await userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);

        if (!UserAccessRules.IsVisible(target, command.CallerId, command.CallerRole) || target is null)
        {
            return false;
        }

        if (command.CallerRole == UserRole.Owner && target.Id == command.CallerId)
        {
            throw new DomainRuleException("RF-PRO-09", "No puede eliminar su propia cuenta.");
        }


        target.Delete(Guid.NewGuid(), timeProvider.GetUtcNow());
        await userRepository.UpdateAsync(target, cancellationToken);

        return true;
    }
}
