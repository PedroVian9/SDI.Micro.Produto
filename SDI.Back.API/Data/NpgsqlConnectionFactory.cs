using Npgsql;

namespace SDI.Back.API.Data;

public sealed class NpgsqlConnectionFactory(NpgsqlDataSource dataSource) : IDbConnectionFactory
{
    public ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        return dataSource.OpenConnectionAsync(cancellationToken);
    }
}
