using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Veterinaria.Application.Authentication;
using Veterinaria.Infrastructure.Identity;
using Xunit;
using DomainLockoutOptions = Veterinaria.Domain.Configuration.LockoutOptions;

namespace Veterinaria.Domain.Tests;

// RN-S5: after 3 failed password attempts the account is locked for 2 minutes; a 4th attempt, even with the
// correct password, is rejected while the lock lasts.
//
// Identity reads DateTimeOffset.UtcNow internally when it stamps the lockout end, so the clock cannot be injected.
// The test therefore does not wait 2 minutes: it asserts the lockout end date Identity stores, and that the 4th
// attempt is rejected because that date is still in the future.
public class AccountLockoutTests
{
    private const string Email = "owner@lockout.test";
    private const string CorrectPassword = "Correct-Passw0rd";
    private const string WrongPassword = "Wrong-Passw0rd";

    [Fact]
    public async Task FourthAttempt_IsRejected_WhileLockoutLasts_AfterThreeFailedAttempts()
    {
        // Same values as the RN-S5 configuration, mapped by the real IdentityConfiguration.Apply.
        var lockout = new DomainLockoutOptions { MaxFailedAttempts = 3, LockoutMinutes = 2 };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        services.Configure<IdentityOptions>(options => IdentityConfiguration.Apply(options, lockout));
        services.AddSingleton<IUserStore<ApplicationUser>, InMemoryUserStore>();
        services.AddIdentityCore<ApplicationUser>().AddSignInManager();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var gateway = new IdentityAuthenticationGateway(userManager, signInManager);

        var user = new ApplicationUser { UserName = Email, Email = Email, Name = "Owner", Identification = "1" };
        var created = await userManager.CreateAsync(user, CorrectPassword);
        Assert.True(created.Succeeded);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var failed = await gateway.CheckCredentialsAsync(Email, WrongPassword, CancellationToken.None);
            // The third failure reaches MaxFailedAccessAttempts, so Identity locks the account on that same attempt.
            var expected = attempt < 3 ? LoginOutcome.InvalidCredentials : LoginOutcome.LockedOut;
            Assert.Equal(expected, failed.Outcome);
        }

        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
        Assert.NotNull(lockoutEnd);
        var now = DateTimeOffset.UtcNow;
        Assert.True(lockoutEnd > now, "The lockout end date must be in the future right after the third failure.");
        Assert.True(lockoutEnd <= now.AddMinutes(2), "The lockout must not exceed the configured 2 minutes.");
        Assert.True(lockoutEnd >= now.AddMinutes(2).AddSeconds(-5), "The lockout must last the configured 2 minutes.");

        var rejected = await gateway.CheckCredentialsAsync(Email, CorrectPassword, CancellationToken.None);

        Assert.Equal(LoginOutcome.LockedOut, rejected.Outcome);
    }

    // RN-05 / D-4 (R15-1): attempts made during the lockout, correct or not, do not extend it and do not count.
    [Fact]
    public async Task AttemptsDuringLockout_DoNotExtendLockoutEnd_NorIncrementFailedCount()
    {
        var lockout = new DomainLockoutOptions { MaxFailedAttempts = 3, LockoutMinutes = 2 };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthentication();
        services.Configure<IdentityOptions>(options => IdentityConfiguration.Apply(options, lockout));
        services.AddSingleton<IUserStore<ApplicationUser>, InMemoryUserStore>();
        services.AddIdentityCore<ApplicationUser>().AddSignInManager();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var gateway = new IdentityAuthenticationGateway(userManager, signInManager);

        var user = new ApplicationUser { UserName = Email, Email = Email, Name = "Owner", Identification = "1" };
        Assert.True((await userManager.CreateAsync(user, CorrectPassword)).Succeeded);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await gateway.CheckCredentialsAsync(Email, WrongPassword, CancellationToken.None);
        }

        var lockoutEndBefore = await userManager.GetLockoutEndDateAsync(user);
        var failedBefore = user.AccessFailedCount;

        var wrongDuring = await gateway.CheckCredentialsAsync(Email, WrongPassword, CancellationToken.None);
        var correctDuring = await gateway.CheckCredentialsAsync(Email, CorrectPassword, CancellationToken.None);

        Assert.Equal(LoginOutcome.LockedOut, wrongDuring.Outcome);
        Assert.Equal(LoginOutcome.LockedOut, correctDuring.Outcome);
        Assert.Equal(lockoutEndBefore, await userManager.GetLockoutEndDateAsync(user));
        Assert.Equal(failedBefore, user.AccessFailedCount);
        Assert.Equal(lockoutEndBefore, correctDuring.LockoutEnd);
    }}

// Minimal in-memory Identity store used only by this test. It keeps the users in a list and implements the
// interfaces that UserManager, SignInManager and the lockout logic need. Passwords are hashed by Identity itself.
internal sealed class InMemoryUserStore :
    IUserStore<ApplicationUser>,
    IUserPasswordStore<ApplicationUser>,
    IUserLockoutStore<ApplicationUser>,
    IUserEmailStore<ApplicationUser>
{
    private readonly List<ApplicationUser> _users = [];

    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (user.Id == Guid.Empty)
        {
            user.Id = Guid.NewGuid();
        }

        _users.Add(user);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        _users.Remove(user);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id.ToString() == userId));

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(u => u.NormalizedUserName == normalizedUserName));

    public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(u => u.NormalizedEmail == normalizedEmail));

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash is not null);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.LockoutEnabled);

    public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
