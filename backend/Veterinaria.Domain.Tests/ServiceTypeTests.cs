using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Infrastructure.Persistence.Migrations;
using Xunit;

namespace Veterinaria.Domain.Tests;

public class ServiceTypeTests
{
    [Fact]
    public void Aesthetics_CanBeUsedForServiceCreationAndUpdate()
    {
        var service = new Service(Guid.NewGuid(), "Estética", ServiceType.Aesthetics);

        Assert.Equal(ServiceType.Aesthetics, service.Type);

        service.Update("Estética personalizada", ServiceType.Clinic);
        service.Update("Estética personalizada", ServiceType.Aesthetics);

        Assert.Equal(ServiceType.Aesthetics, service.Type);
    }

    [Fact]
    public void ServiceTypes_UseNewTextContract()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));

        Assert.Equal(new[] { "Clinic", "Aesthetics", "Recreation" }, Enum.GetNames<ServiceType>());
        Assert.Equal("\"Aesthetics\"", JsonSerializer.Serialize(ServiceType.Aesthetics, options));
        Assert.Equal(ServiceType.Aesthetics, JsonSerializer.Deserialize<ServiceType>("\"Aesthetics\"", options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ServiceType>("\"Grooming\"", options));
    }

    [Fact]
    public void Migration_ConvertsTypesBeforeRestoringConstraints_InBothDirections()
    {
        var migration = new RenameGroomingToAesthetics();

        Assert.Collection(migration.UpOperations,
            operation => Assert.IsType<DropCheckConstraintOperation>(operation),
            operation => Assert.Equal("UPDATE \"Services\" SET \"Type\" = 'Aesthetics' WHERE \"Type\" = 'Grooming';", Assert.IsType<SqlOperation>(operation).Sql),
            operation => Assert.Contains("'Aesthetics'", Assert.IsType<AddCheckConstraintOperation>(operation).Sql));

        Assert.Collection(migration.DownOperations,
            operation => Assert.IsType<DropCheckConstraintOperation>(operation),
            operation => Assert.Equal("UPDATE \"Services\" SET \"Type\" = 'Grooming' WHERE \"Type\" = 'Aesthetics';", Assert.IsType<SqlOperation>(operation).Sql),
            operation => Assert.Contains("'Grooming'", Assert.IsType<AddCheckConstraintOperation>(operation).Sql));
    }
}