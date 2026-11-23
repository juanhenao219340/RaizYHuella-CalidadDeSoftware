namespace Veterinaria.Domain.Configuration;

public sealed class BusinessRulesOptions
{

    public int SlotCapacity { get; set; }

    public string TimeZone { get; set; } = string.Empty;

    public TimeZoneInfo GetTimeZone() => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
}
