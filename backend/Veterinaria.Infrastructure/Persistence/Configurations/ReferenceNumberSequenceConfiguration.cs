using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Veterinaria.Infrastructure.Persistence.Models;

namespace Veterinaria.Infrastructure.Persistence.Configurations;

public sealed class ReferenceNumberSequenceConfiguration : IEntityTypeConfiguration<ReferenceNumberSequence>
{
    public void Configure(EntityTypeBuilder<ReferenceNumberSequence> builder)
    {
        builder.ToTable("ReferenceNumberSequences", table =>
            table.HasCheckConstraint("CK_ReferenceNumberSequences_LastNumber", @"""LastNumber"" BETWEEN 0 AND 9999"));

        builder.HasKey(sequence => sequence.Date);
        builder.Property(sequence => sequence.LastNumber).IsRequired();
    }
}
