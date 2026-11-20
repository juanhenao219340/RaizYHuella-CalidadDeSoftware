using System.Globalization;
using System.Text.RegularExpressions;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Domain.ValueObjects;

public readonly record struct ReferenceNumber
{
    private static readonly Regex Pattern = new(@"^RAD-[0-9]{8}-[0-9]{4}\z", RegexOptions.CultureInvariant);

    private ReferenceNumber(string value) => Value = value;

    public string Value { get; }

    public static ReferenceNumber Parse(string? value)
    {
        if (value is null || !Pattern.IsMatch(value))
        {
            throw new DomainRuleException("RF-PRO-03", "El formato del radicado debe ser RAD-AAAAMMDD-####.");
        }

        var parts = value.Split('-');
        var validDate = DateOnly.TryParseExact(parts[1], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        var sequence = int.Parse(parts[2], CultureInfo.InvariantCulture);

        if (!validDate || sequence < 1)
        {
            throw new DomainRuleException("RF-PRO-03", "El formato del radicado debe ser RAD-AAAAMMDD-####.");
        }

        return new ReferenceNumber(value);
    }

    public override string ToString() => Value;
}
