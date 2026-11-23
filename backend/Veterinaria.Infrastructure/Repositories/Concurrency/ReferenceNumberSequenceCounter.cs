using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Veterinaria.Infrastructure.Persistence;

namespace Veterinaria.Infrastructure.Repositories.Concurrency;




public sealed class ReferenceNumberSequenceCounter(VeterinaryDbContext context)
{
    public async Task<int> NextAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "ReferenceNumberSequences" ("Date", "LastNumber")
            VALUES (@date, 1)
            ON CONFLICT ("Date") DO UPDATE SET "LastNumber" = "ReferenceNumberSequences"."LastNumber" + 1
            RETURNING "LastNumber";
            """;
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        var parameter = command.CreateParameter();
        parameter.ParameterName = "date";
        parameter.Value = date;
        command.Parameters.Add(parameter);

        try
        {
            var value = await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (PersistenceErrors.Translate(exception) is { } translated)
        {
            throw translated;
        }
    }
}
