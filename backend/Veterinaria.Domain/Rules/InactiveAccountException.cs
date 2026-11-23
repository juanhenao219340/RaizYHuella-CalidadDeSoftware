namespace Veterinaria.Domain.Rules;


public sealed class InactiveAccountException() : Exception("La cuenta ya no está activa.");
