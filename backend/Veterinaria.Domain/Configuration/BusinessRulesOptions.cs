namespace Veterinaria.Domain.Configuration;

public sealed class BusinessRulesOptions
{
    // Required from the environment (RNF-CFG-01). No default: the start-up validator rejects a missing value.
    public int SlotCapacity { get; set; }

    public string TimeZone { get; set; } = string.Empty;

    public TimeZoneInfo GetTimeZone() => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
}
