namespace Veterinaria.Api.Authentication;

public static class AuthenticationConstants
{

    public const string Issuer = "veterinaria-api";
    public const string Audience = "veterinaria-web";
    public const string RoleClaim = "role";
    public const int TokenLifetimeMinutes = 60;
}
