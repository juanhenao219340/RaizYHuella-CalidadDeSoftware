namespace Veterinaria.Infrastructure.Persistence.Models;

public sealed class ReferenceNumberSequence
{
    public DateOnly Date { get; set; }

    public int LastNumber { get; set; }
}
