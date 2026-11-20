using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Contracts;

// Rejects values that start with the prefix reserved for logically deleted records (OD-2, case-sensitive: S-26, D-9).
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class NotReservedPrefixAttribute : ValidationAttribute
{
    public const string ReservedPrefix = "DEL-";

    public NotReservedPrefixAttribute()
        : base("El valor no puede empezar por DEL-, prefijo reservado por el sistema.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text || !text.StartsWith(ReservedPrefix, StringComparison.Ordinal);
}
