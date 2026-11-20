using System.Globalization;
using Veterinaria.Domain.ValueObjects;

namespace Veterinaria.Domain.Rules;

public static class ReferenceNumberGenerator
{
    public const int MaxSequence = 9999;

    public static ReferenceNumber Format(DateOnly registrationDate, int sequence)
    {
        if (sequence < 1 || sequence > MaxSequence)
        {
            throw new DomainRuleException("RF-PRO-03", "El consecutivo diario del radicado está fuera del rango permitido (1 a 9999).");
        }

        var date = registrationDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var number = sequence.ToString("D4", CultureInfo.InvariantCulture);

        return ReferenceNumber.Parse($"RAD-{date}-{number}");
    }
}
