using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Authentication;
using Veterinaria.Api.Controllers;
using Veterinaria.Application.Abstractions;
using Veterinaria.Application.Appointments;
using Veterinaria.Domain.Enumerations;
using Xunit;

namespace Veterinaria.Domain.Tests;

public class AppointmentPetNameFilterTests
{
    [Theory]
    [InlineData(UserRole.Administrator)]
    [InlineData(UserRole.Owner)]
    public async Task PetName_IsTrimmedAndCombinedWithFilters_WhilePreservingRoleScope(UserRole role)
    {
        var callerId = Guid.NewGuid();
        var repository = new RecordingSearchRepository();
        var controller = CreateController(repository, callerId, role,
            "?petName=%20LuNa%20&referenceNumber=RAD-1&serviceType=Clinic");

        var result = await controller.List(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var criteria = Assert.IsType<AppointmentSearchCriteria>(repository.Criteria);
        Assert.Equal("LuNa", criteria.PetName);
        Assert.Equal("RAD-1", criteria.ReferenceNumber);
        Assert.Equal(ServiceType.Clinic, criteria.ServiceType);
        Assert.Equal(role == UserRole.Owner ? callerId : null, criteria.OwnerId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("%20%20")]
    public async Task EmptyPetName_IsTreatedAsAbsent(string value)
    {
        var repository = new RecordingSearchRepository();
        var controller = CreateController(repository, Guid.NewGuid(), UserRole.Owner, $"?petName={value}");

        Assert.IsType<OkObjectResult>(await controller.List(CancellationToken.None));
        Assert.Null(Assert.IsType<AppointmentSearchCriteria>(repository.Criteria).PetName);
    }

    [Fact]
    public async Task PetName_WithOneHundredCharacters_IsAccepted()
    {
        var repository = new RecordingSearchRepository();
        var name = new string('a', 100);
        var controller = CreateController(repository, Guid.NewGuid(), UserRole.Owner, $"?petName={name}");

        Assert.IsType<OkObjectResult>(await controller.List(CancellationToken.None));
        Assert.Equal(name, Assert.IsType<AppointmentSearchCriteria>(repository.Criteria).PetName);
    }

    [Theory]
    [InlineData("?petName=Luna&petName=Sol")]
    [InlineData("?petName=" + "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task InvalidPetName_ReturnsValidationErrorWithoutSearching(string query)
    {
        var repository = new RecordingSearchRepository();
        var controller = CreateController(repository, Guid.NewGuid(), UserRole.Owner, query);

        var result = Assert.IsType<BadRequestObjectResult>(await controller.List(CancellationToken.None));
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Contains("petName", problem.Errors.Keys);
        Assert.Null(repository.Criteria);
    }

    private static AppointmentsController CreateController(
        RecordingSearchRepository repository, Guid callerId, UserRole role, string query)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, callerId.ToString()),
                new Claim(AuthenticationConstants.RoleClaim, role.ToString())
            ], "test"))
        };
        context.Request.QueryString = new QueryString(query);
        return new AppointmentsController(null!, new ListAppointmentsHandler(repository), null!, null!, null!)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private sealed class RecordingSearchRepository : IAppointmentSearchRepository
    {
        public AppointmentSearchCriteria? Criteria { get; private set; }

        public Task<IReadOnlyList<AppointmentListItem>> SearchAsync(
            AppointmentSearchCriteria criteria, CancellationToken cancellationToken)
        {
            Criteria = criteria;
            return Task.FromResult<IReadOnlyList<AppointmentListItem>>([]);
        }
    }
}
