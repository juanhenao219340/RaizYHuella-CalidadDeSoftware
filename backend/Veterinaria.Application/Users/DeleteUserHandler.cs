using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Application.Users;

public sealed record DeleteUserCommand(Guid TargetUserId, Guid CallerId, UserRole CallerRole);

public sealed class DeleteUserHandler(IUserRepository userRepository, TimeProvider timeProvider)
{
    // Returns false when the account is missing, deleted, or not visible to the caller (the API answers 404).
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

        // The row is never removed: the e-mail and identification are replaced so the original values can be reused (RF-REG-10).
        target.Delete(Guid.NewGuid(), timeProvider.GetUtcNow());
        await userRepository.UpdateAsync(target, cancellationToken);

        return true;
    }
}
