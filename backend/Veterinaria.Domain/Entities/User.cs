using System.Text.RegularExpressions;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Domain.Entities;

public class User
{
    private static readonly Regex EmailFormat = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+\z", RegexOptions.CultureInvariant);

    public Guid Id { get; private set; }

    public string Identification { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public UserRole Role { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;


    private User()
    {
    }

    public User(Guid id, string identification, string name, string email, string? phone, UserRole role)
    {
        var candidate = new User
        {
            Id = id,
            Identification = identification,
            Name = name,
            Email = email,
            Phone = phone,
            Role = role
        };

        Validate(candidate).ThrowIfFailed();

        Id = candidate.Id;
        Identification = candidate.Identification;
        Name = candidate.Name;
        Email = candidate.Email;
        Phone = candidate.Phone;
        Role = candidate.Role;
    }


    public static User Reconstitute(
        Guid id,
        string identification,
        string name,
        string email,
        string? phone,
        UserRole role,
        DateTimeOffset? deletedAt) =>
        new()
        {
            Id = id,
            Identification = identification,
            Name = name,
            Email = email,
            Phone = phone,
            Role = role,
            DeletedAt = deletedAt
        };

    public void Update(string name, string email, string? phone, UserRole role)
    {
        if (IsDeleted)
        {
            throw new DomainRuleException("RF-REG-10", "El usuario está eliminado y no puede modificarse.");
        }

        var candidate = new User
        {
            Id = Id,
            Identification = Identification,
            Name = name,
            Email = email,
            Phone = phone,
            Role = role
        };

        Validate(candidate).ThrowIfFailed();

        Name = candidate.Name;
        Email = candidate.Email;
        Phone = candidate.Phone;
        Role = candidate.Role;
    }


    public void Delete(Guid releaseToken, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainRuleException("RF-REG-10", "El usuario ya está eliminado.");
        }

        Identification = $"DEL-{releaseToken:N}"[..20];
        Email = $"deleted-{releaseToken:N}@deleted.invalid";
        DeletedAt = deletedAt;
    }

    public static DomainResult Validate(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Identification) || string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Email))
        {
            return DomainResult.Failed("RF-REG-05", "La identificación, el nombre y el correo del usuario son obligatorios.");
        }

        if (!EmailFormat.IsMatch(user.Email))
        {
            return DomainResult.Failed("RF-REG-05", "El correo del usuario no tiene un formato válido.");
        }

        return DomainResult.Success();
    }
}
