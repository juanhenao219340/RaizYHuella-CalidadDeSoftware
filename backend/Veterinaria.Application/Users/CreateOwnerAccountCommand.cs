namespace Veterinaria.Application.Users;

public sealed record CreateOwnerAccountCommand(
    string Identification,
    string Name,
    string Email,
    string? Phone,
    string Password);
