namespace Veterinaria.Api.Authentication;

public static class AuthenticationConstants
{
    // Fixed by the T-04 plan r3 (OD-2r3). The token travels only in the Authorization: Bearer header.
    public const string Issuer = "veterinaria-api";
    public const string Audience = "veterinaria-web";
    public const string RoleClaim = "role";
    public const int TokenLifetimeMinutes = 60;
}
