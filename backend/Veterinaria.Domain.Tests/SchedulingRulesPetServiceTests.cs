using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;
using Veterinaria.Domain.ValueObjects;
using Xunit;
using static Veterinaria.Domain.Tests.TestData;

namespace Veterinaria.Domain.Tests;

public class SchedulingRulesPetServiceTests
{
    [Fact]
    public void Validate_SamePetSameTupleActive_ReturnsP02()
    {
        var petId = Guid.NewGuid();
        var appointments = new List<AppointmentSummary> { Summary(petId: petId, hour: 10, serviceId: ServiceA) };

        var result = SchedulingRules.Validate(Request(petId: petId, hour: 10, serviceId: ServiceA), NowUtc, Options(), appointments, null);

        AssertFailed(result, "P-02");
    }

    [Fact]
    public void Validate_SamePetSameTupleCancelled_IsAccepted()
    {
        var petId = Guid.NewGuid();
        var appointments = new List<AppointmentSummary>
        {
            Summary(AppointmentStatus.Cancelled, petId: petId, hour: 10, serviceId: ServiceA)
        };

        var result = SchedulingRules.Validate(Request(petId: petId, hour: 10, serviceId: ServiceA), NowUtc, Options(), appointments, null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_SamePetSameDateAndHourOtherService_IsAccepted()
    {
        var petId = Guid.NewGuid();
        var appointments = new List<AppointmentSummary> { Summary(petId: petId, hour: 10, serviceId: ServiceB) };

        var result = SchedulingRules.Validate(Request(petId: petId, hour: 10, serviceId: ServiceA), NowUtc, Options(), appointments, null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_OtherPetSameTuple_IsNotP02()
    {
        // P-02 is per pet. Another pet in the same tuple only consumes capacity, and one slot is still free.
        var appointments = new List<AppointmentSummary> { Summary(petId: Guid.NewGuid(), hour: 10, serviceId: ServiceA) };

        var result = SchedulingRules.Validate(Request(petId: Guid.NewGuid(), hour: 10, serviceId: ServiceA), NowUtc, Options(), appointments, null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Update_ConflictWithSamePetOtherAppointment_ReturnsP02()
    {
        var petId = Guid.NewGuid();
        var first = Appointment.Schedule(
            Guid.NewGuid(),
            ReferenceNumber.Parse("RAD-20261119-0200"),
            Request(petId: petId, hour: 10),
            NowUtc,
            Options(),
            Array.Empty<AppointmentSummary>());
        var second = Appointment.Schedule(
            Guid.NewGuid(),
            ReferenceNumber.Parse("RAD-20261119-0201"),
            Request(petId: petId, hour: 11),
            NowUtc,
            Options(),
            new[] { Summarize(first) });

        // Moving the first appointment to 11:00 with the same service collides with the second appointment of the same pet.
        var summaries = new[] { Summarize(first), Summarize(second) };
        var result = AppointmentModificationRules.ValidateUpdate(
            UserRole.Administrator,
            first,
            "Reprogramación",
            Request(hour: 11, serviceId: ServiceA),
            NowUtc,
            Options(),
            summaries);

        AssertFailed(result, "P-02");
    }

    [Fact]
    public void Update_OwnAppointmentIsNotCountedAsConflict()
    {
        var appointment = Appointment.Schedule(
            Guid.NewGuid(),
            ReferenceNumber.Parse("RAD-20261119-0202"),
            Request(hour: 10),
            NowUtc,
            Options(),
            Array.Empty<AppointmentSummary>());

        // Same tuple as the appointment itself: it must not conflict with its own record.
        var result = AppointmentModificationRules.ValidateUpdate(
            UserRole.Administrator,
            appointment,
            "Reprogramación",
            Request(hour: 10, serviceId: ServiceA),
            NowUtc,
            Options(),
            new[] { Summarize(appointment) });

        Assert.True(result.IsSuccess);
    }
}
