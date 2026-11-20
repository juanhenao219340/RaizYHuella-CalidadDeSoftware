using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Pets;

// Read model returned by the pet queries (RF-REG-07 a, R14-1). Not a domain entity and never persisted.
// OwnerName and OwnerDeleted are null for owners (no owner data is loaded) and when the owner row is missing (D-3).
public sealed record PetWithOwner(Pet Pet, string? OwnerName, bool? OwnerDeleted);
