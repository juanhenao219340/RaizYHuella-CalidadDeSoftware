using Microsoft.Extensions.Options;
using Veterinaria.Domain.Configuration;

namespace Veterinaria.Infrastructure.Configuration;

public sealed class BusinessRulesOptionsValidator : IValidateOptions<BusinessRulesOptions>
{
    public ValidateOptionsResult Validate(string? name, BusinessRulesOptions options)
    {
        if (options.SlotCapacity <= 0)
        {
            return ValidateOptionsResult.Fail("BusinessRules:SlotCapacity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.TimeZone))
        {
            return ValidateOptionsResult.Fail("BusinessRules:TimeZone (BusinessRules__TimeZone) is required.");
        }

        try
        {
            _ = options.GetTimeZone();
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return ValidateOptionsResult.Fail($"BusinessRules:TimeZone '{options.TimeZone}' cannot be resolved on this host.");
        }

        return ValidateOptionsResult.Success;
    }
}
