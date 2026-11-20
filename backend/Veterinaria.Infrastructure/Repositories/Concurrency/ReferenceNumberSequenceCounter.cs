using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Veterinaria.Infrastructure.Persistence;

namespace Veterinaria.Infrastructure.Repositories.Concurrency;

// EXCEPCIÓN R13-3 (RNF-ARQ-07): única SQL escrita a mano permitida en backend/.
// Consecutivo diario atómico sobre "ReferenceNumberSequences" (INSERT ... ON CONFLICT ... DO UPDATE ... RETURNING),
// parametrizado y ejecutado dentro de la transacción actual de EF Core. Si la transacción hace rollback, el consecutivo también.
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
