using Veterinaria.Domain.Entities;

namespace Veterinaria.Application.Pets;



public sealed record PetWithOwner(Pet Pet, string? OwnerName, bool? OwnerDeleted);
