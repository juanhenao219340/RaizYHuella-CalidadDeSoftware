namespace Veterinaria.Domain.Rules;

// Raised when the owner of an appointment was deleted during the operation (RF-AUT-07). Mapped to 401.
public sealed class InactiveAccountException() : Exception("La cuenta ya no está activa.");
