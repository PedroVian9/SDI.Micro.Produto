using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace SDI.Back.Template.HealthChecks;

public sealed class PostgresHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            var result = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("select 1", cancellationToken: cancellationToken));

            return result == 1
                ? HealthCheckResult.Healthy("Postgres respondendo.")
                : HealthCheckResult.Unhealthy("Postgres respondeu de forma inesperada.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Nao foi possivel conectar ao Postgres.", ex);
        }
    }
}
