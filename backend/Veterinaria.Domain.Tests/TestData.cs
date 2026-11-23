using Veterinaria.Domain.Configuration;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;
using Xunit;

namespace Veterinaria.Domain.Tests;


internal static class TestData
{

    public static readonly DateTimeOffset NowUtc = new(2026, 11, 18, 17, 0, 0, TimeSpan.Zero);


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
