using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Domain.Entities;

public class Service
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ServiceType Type { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    private Service()
    {
    }

    public Service(Guid id, string name, ServiceType type)
    {
        var candidate = new Service
        {
            Id = id,
            Name = name,
            Type = type
        };

        Validate(candidate).ThrowIfFailed();

        Id = candidate.Id;
        Name = candidate.Name;
        Type = candidate.Type;
    }

    public void Update(string name, ServiceType type)
    {
        if (IsDeleted)
        {
            throw new DomainRuleException("RF-REG-10", "El servicio está eliminado y no puede modificarse.");
        }

        var candidate = new Service
        {
            Id = Id,
            Name = name,
            Type = type
        };

        Validate(candidate).ThrowIfFailed();

        Name = candidate.Name;
        Type = candidate.Type;
    }

    // Logical deletion (RF-REG-10). Services have no uniqueness constraint, so no value is transformed.
    public void Delete(DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainRuleException("RF-REG-10", "El servicio ya está eliminado.");
        }

        DeletedAt = deletedAt;
    }

    public static DomainResult Validate(Service service)
    {
        if (string.IsNullOrWhiteSpace(service.Name))
        {
            return DomainResult.Failed("RF-REG-05", "El nombre del servicio es obligatorio.");
        }

        if (!Enum.IsDefined(service.Type))
        {
            return DomainResult.Failed("RF-REG-05", "El tipo de servicio no es válido.");
        }

        return DomainResult.Success();
    }
}
