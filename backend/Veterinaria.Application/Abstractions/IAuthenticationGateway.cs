using Veterinaria.Application.Authentication;

namespace Veterinaria.Application.Abstractions;

public interface IAuthenticationGateway
{
    Task<LoginResult> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken);
}
