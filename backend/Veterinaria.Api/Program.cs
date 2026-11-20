using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Veterinaria.Api.Authentication;
using Veterinaria.Api.Configuration;
using Veterinaria.Api.Middleware;
using Veterinaria.Application.Abstractions;
using Veterinaria.Application.Appointments;
using Veterinaria.Application.Pets;
using Veterinaria.Application.Services;
using Veterinaria.Application.Users;
using Veterinaria.Domain.Configuration;
using Veterinaria.Infrastructure;
using Veterinaria.Infrastructure.Configuration;
using Veterinaria.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddInfrastructure(configuration);

builder.Services.AddOptions<JwtOptions>()
    .Bind(configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<BasicAuthOptions>()
    .Bind(configuration.GetSection(BasicAuthOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CorsOptions>()
    .Bind(configuration.GetSection(CorsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<BasicCredentialsVerifier>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<CreateOwnerAccountHandler>();

// Application handlers (T-05). Controllers only map DTOs to commands and translate null to 404.
builder.Services.AddScoped<GetUserHandler>();
builder.Services.AddScoped<UpdateUserHandler>();
builder.Services.AddScoped<DeleteUserHandler>();
builder.Services.AddScoped<ListOwnersHandler>();
builder.Services.AddScoped<GetCurrentUserHandler>();
builder.Services.AddScoped<UpdateCurrentUserHandler>();
builder.Services.AddScoped<CreatePetHandler>();
builder.Services.AddScoped<UpdatePetHandler>();
builder.Services.AddScoped<GetPetHandler>();
builder.Services.AddScoped<ListPetsHandler>();
builder.Services.AddScoped<DeletePetHandler>();
builder.Services.AddScoped<CreateServiceHandler>();
builder.Services.AddScoped<UpdateServiceHandler>();
builder.Services.AddScoped<GetServiceHandler>();
builder.Services.AddScoped<ListServicesHandler>();
builder.Services.AddScoped<DeleteServiceHandler>();

// Appointment handlers (T-06). Business rules are read once from configuration; no HTTP types in Application.
builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<BusinessRulesOptions>>().Value);
builder.Services.AddScoped<GetAvailabilityHandler>();
builder.Services.AddScoped<ListAppointmentsHandler>();
builder.Services.AddScoped<CreateAppointmentHandler>();
builder.Services.AddScoped<RescheduleAppointmentHandler>();
builder.Services.AddScoped<CancelAppointmentHandler>();

// JWT is read from the Authorization: Bearer header only (default JwtBearer behaviour, D2 and D11).
// No cookie, query string or custom token event is accepted. The events below never read the token;
// they only check the account state (RF-AUT-07) and write Spanish 401 bodies (OD-3, T-05 paso 6).
const string accountInactiveItem = "AccountInactive";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwt) =>
    {
        options.MapInboundClaims = false;

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindUserId();

                if (userId is null)
                {
                    context.Fail("Token sin sujeto válido.");
                    return;
                }

                var gate = context.HttpContext.RequestServices.GetRequiredService<IUserAccessGate>();

                if (!await gate.IsActiveAsync(userId.Value, context.HttpContext.RequestAborted))
                {
                    context.HttpContext.Items[accountInactiveItem] = true;
                    context.Fail("Cuenta inactiva.");
                }
            },
            OnChallenge = async context =>
            {
                // The WWW-Authenticate: Bearer header written by JwtBearer is kept (not the browser Basic dialog).
                context.HandleResponse();

                var inactive = context.HttpContext.Items.TryGetValue(accountInactiveItem, out var flag) && flag is true;

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = inactive ? "La cuenta ya no está activa." : "Sesión no válida o caducada.",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
                });
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Key)),
            ValidateIssuer = true,
            ValidIssuer = AuthenticationConstants.Issuer,
            ValidateAudience = true,
            ValidAudience = AuthenticationConstants.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = "sub",
            RoleClaimType = AuthenticationConstants.RoleClaim
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("Owner", policy => policy.RequireRole("Owner"));

    // Every endpoint requires authentication unless it is explicitly [AllowAnonymous] (RNF-SEG-03).
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

// Policy failures (403) get a Spanish ProblemDetails body instead of the framework default (RNF-IDI-01, D-12).
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ForbiddenResultHandler>();

// Only the frontend origin is allowed. No credentials: the token is sent explicitly in the Authorization header (OD-2r3).
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins(configuration[$"{CorsOptions.SectionName}:AllowedOrigin"] ?? string.Empty)
    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
    .WithHeaders("Content-Type", "Authorization")));

// Enums are read and written as text: Clinic, Grooming, Recreation (D-10).
builder.Services.AddControllers(options =>
    {
        // Framework-generated binding messages are translated at the source (RNF-IDI-01, D-12).
        // System.Text.Json conversion text that reaches ModelState without an exception is replaced in the factory below.
        var messages = options.ModelBindingMessageProvider;
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "El cuerpo de la solicitud es obligatorio.");
        messages.SetMissingBindRequiredValueAccessor(name => $"El campo '{name}' es obligatorio.");
        messages.SetMissingKeyOrValueAccessor(() => "Falta un valor obligatorio.");
        messages.SetValueIsInvalidAccessor(value => $"El valor '{value}' no es válido.");
        messages.SetAttemptedValueIsInvalidAccessor((value, name) => $"El valor '{value}' no es válido para '{name}'.");
        messages.SetUnknownValueIsInvalidAccessor(name => $"El valor proporcionado para '{name}' no es válido.");
        messages.SetValueMustNotBeNullAccessor(value => $"El valor '{value}' no es válido.");
        messages.SetValueMustBeANumberAccessor(name => $"El campo '{name}' debe ser un número.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"El valor '{value}' no es válido.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor proporcionado no es válido.");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "El valor debe ser un número.");
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// Remaining framework text (e.g. System.Text.Json conversion errors reported without an exception) is replaced by a
// Spanish message so no framework text reaches the interface (RNF-IDI-01, D-12). DataAnnotations messages are kept.
// The translation itself lives in ModelErrorTranslator (shared with controllers that validate manually).
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = context =>
    {
        return new BadRequestObjectResult(ModelErrorTranslator.CreateValidationProblem(context.ModelState, context.HttpContext));
    });


builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

// Fail before migrating or listening if a required setting is missing or invalid (CA-EJE-04).
EnsureOptionsValid<DatabaseOptions>(app.Services);
EnsureOptionsValid<BusinessRulesOptions>(app.Services);
EnsureOptionsValid<LockoutOptions>(app.Services);
EnsureOptionsValid<JwtOptions>(app.Services);
EnsureOptionsValid<BasicAuthOptions>(app.Services);
EnsureOptionsValid<CorsOptions>(app.Services);

// Migrations and idempotent seed run on every start-up (RNF-EJE-02, RF-AUT-09).
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<SeedRunner>().RunAsync(CancellationToken.None);
}

app.UseExceptionHandler();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static void EnsureOptionsValid<T>(IServiceProvider services) where T : class =>
    _ = services.GetRequiredService<IOptions<T>>().Value;
