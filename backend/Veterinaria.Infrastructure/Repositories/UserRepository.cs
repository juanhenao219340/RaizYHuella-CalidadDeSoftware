using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Rules;
using Veterinaria.Infrastructure.Identity;
using Veterinaria.Infrastructure.Persistence;

namespace Veterinaria.Infrastructure.Repositories;

public sealed class UserRepository(VeterinaryDbContext context, UserManager<ApplicationUser> userManager) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var applicationUser = await userManager.FindByIdAsync(id.ToString());

        return applicationUser is null ? null : await ToDomainAsync(applicationUser);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var applicationUser = await userManager.FindByEmailAsync(email);

        return applicationUser is null ? null : await ToDomainAsync(applicationUser);
    }

    // Owners that are not logically deleted. Deleted accounts keep their transformed values and are not listed (RF-REG-10).
    public async Task<IReadOnlyList<User>> ListOwnersAsync(CancellationToken cancellationToken)
    {
        var owners = await userManager.GetUsersInRoleAsync(RoleNames.Owner);

        return owners
            .Where(owner => owner.DeletedAt is null)
            .OrderBy(owner => owner.Name)
            .Select(owner => UserMapper.ToDomain(owner, [RoleNames.Owner]))
            .ToList();
    }

    public Task<bool> IdentificationExistsAsync(string identification, CancellationToken cancellationToken) =>
        userManager.Users.AnyAsync(user => user.Identification == identification, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        await userManager.FindByEmailAsync(email) is not null;

    public async Task CreateAsync(User user, string password, CancellationToken cancellationToken)
    {
        var applicationUser = new ApplicationUser
        {
            Id = user.Id,
            UserName = user.Email,
            Email = user.Email
        };

        UserMapper.CopyDomainData(user, applicationUser);

        // Account creation and role assignment commit together (H-D): if the role fails, the account is not kept.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var created = await ExecuteAsync(() => userManager.CreateAsync(applicationUser, password));
        ThrowIfFailed(created);

        var roleAdded = await ExecuteAsync(() => userManager.AddToRoleAsync(applicationUser, RoleNames.FromRole(user.Role)));
        ThrowIfFailed(roleAdded);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        var applicationUser = await userManager.FindByIdAsync(user.Id.ToString())
            ?? throw new InvalidOperationException($"User '{user.Id}' was not found while updating.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        if (applicationUser.Email != user.Email)
        {
            ThrowIfFailed(await ExecuteAsync(() => userManager.SetEmailAsync(applicationUser, user.Email)));
            ThrowIfFailed(await ExecuteAsync(() => userManager.SetUserNameAsync(applicationUser, user.Email)));
        }

        UserMapper.CopyDomainData(user, applicationUser);
        ThrowIfFailed(await ExecuteAsync(() => userManager.UpdateAsync(applicationUser)));

        await SynchronizeRoleAsync(applicationUser, RoleNames.FromRole(user.Role));

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<User> ToDomainAsync(ApplicationUser applicationUser)
    {
        var roles = await userManager.GetRolesAsync(applicationUser);

        return UserMapper.ToDomain(applicationUser, roles);
    }

    private async Task SynchronizeRoleAsync(ApplicationUser applicationUser, string targetRole)
    {
        var currentRoles = await userManager.GetRolesAsync(applicationUser);

        if (currentRoles.Count == 1 && currentRoles[0] == targetRole)
        {
            return;
        }

        if (currentRoles.Count > 0)
        {
            ThrowIfFailed(await ExecuteAsync(() => userManager.RemoveFromRolesAsync(applicationUser, currentRoles)));
        }

        ThrowIfFailed(await ExecuteAsync(() => userManager.AddToRoleAsync(applicationUser, targetRole)));
    }

    private static async Task<IdentityResult> ExecuteAsync(Func<Task<IdentityResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception) when (PersistenceErrors.Translate(exception) is { } translated)
        {
            throw translated;
        }
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            throw new DomainRuleException("RF-REG-06", "El correo ya está registrado.");
        }

        if (result.Errors.Any(error => error.Code.StartsWith("Password", StringComparison.Ordinal)))
        {
            throw new DomainRuleException(
                "RF-AUT-06",
                "La contraseña debe tener al menos 8 caracteres, con mayúscula, minúscula y número.");
        }

        if (result.Errors.Any(error => error.Code == "ConcurrencyFailure"))
        {
            throw new DomainRuleException(PersistenceErrors.ConcurrencyCode, PersistenceErrors.ConcurrencyMessage);
        }

        throw new InvalidOperationException(
            "Identity operation failed: " + string.Join("; ", result.Errors.Select(error => error.Code)));
    }
}
