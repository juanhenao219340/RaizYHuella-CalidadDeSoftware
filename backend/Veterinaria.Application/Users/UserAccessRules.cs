using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Application.Users;

// Visibility and permission rules for accounts managed through /api/users/{id} (RF-PRO-09, D-2, D-4).
internal static class UserAccessRules
{
    // Returns false when the target is missing, deleted, or not visible to the caller (the API answers 404, D-4).
    // Throws RF-PRO-09 when an administrator targets an administrator account, including their own (D-2).
    public static bool IsVisible(User? target, Guid callerId, UserRole callerRole)
    {
        if (target is null || target.IsDeleted)
        {
            return false;
        }

        if (callerRole == UserRole.Owner)
        {
            return target.Id == callerId;
        }

        if (target.Role == UserRole.Administrator)
        {
            throw new DomainRuleException("RF-PRO-09", "No tiene permiso para administrar esta cuenta.");
        }

        return true;
    }

    // Shared by the account update and the self-service update. The role is always kept (R-2).
    public static async Task<User> ApplyProfileChangeAsync(
        IUserRepository userRepository,
        User target,
        string name,
        string email,
        string? phone,
        CancellationToken cancellationToken)
    {
        var existing = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (existing is not null && existing.Id != target.Id)
        {
            throw new DomainRuleException("RF-REG-06", "El correo ya está registrado.");
        }

        target.Update(name, email, phone, target.Role);
        await userRepository.UpdateAsync(target, cancellationToken);

        return target;
    }
}
