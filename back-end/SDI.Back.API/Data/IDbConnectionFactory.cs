using Npgsql;

namespace SDI.Back.API.Data;

public interface IDbConnectionFactory
{
    ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
