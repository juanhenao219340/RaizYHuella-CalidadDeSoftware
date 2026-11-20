using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;
using Xunit;

namespace Veterinaria.Domain.Tests;

// Shared fixtures. The clock is always injected; no test reads DateTime.Now or UtcNow.
internal static class TestData
{
    // 2026-11-18 12:00 in America/Bogota (UTC-5, no DST), i.e. 17:00 UTC.
    public static readonly DateTimeOffset NowUtc = new(2026, 11, 18, 17, 0, 0, TimeSpan.Zero);

    // A date strictly after "today" so that RN-V1 and P-01 do not interfere with capacity and P-02 checks.
    public static readonly DateOnly FutureDate = new(2026, 11, 19);

    public static readonly Guid ServiceA = new("00000000-0000-0000-0000-00000000000a");
    public static readonly Guid ServiceB = new("00000000-0000-0000-0000-00000000000b");

    public static BusinessRulesOptions Options(int slotCapacity = 4) => new()
    {
        SlotCapacity = slotCapacity,
        TimeZone = "America/Bogota"
    };

    public static AppointmentRequest Request(
        Guid? petId = null,
        int hour = 10,
        Guid? serviceId = null,
        DateOnly? date = null) =>
        new(
            petId ?? Guid.NewGuid(),
            date ?? FutureDate,
            TimeSlot.Create(new TimeOnly(hour, 0)),
            serviceId ?? ServiceA);

    public static AppointmentSummary Summary(
        AppointmentStatus status = AppointmentStatus.Confirmed,
        Guid? petId = null,
        int hour = 10,
        Guid? serviceId = null,
        DateOnly? date = null,
        Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(),
            petId ?? Guid.NewGuid(),
            date ?? FutureDate,
            TimeSlot.Create(new TimeOnly(hour, 0)),
            serviceId ?? ServiceA,
            status);

    // Creates `count` confirmed appointments in the same slot and service, each for a different pet.
    public static List<AppointmentSummary> ConfirmedInSlot(int count, int hour = 10, Guid? serviceId = null) =>
        Enumerable.Range(0, count)
            .Select(_ => Summary(hour: hour, serviceId: serviceId))
            .ToList();

    public static AppointmentSummary Summarize(Appointment appointment) =>
        new(appointment.Id, appointment.PetId, appointment.Date, appointment.StartTime, appointment.ServiceId, appointment.Status);

    public static void AssertFailed(DomainResult result, string expectedCode)
    {
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(expectedCode, result.Error.Code);
    }
}
