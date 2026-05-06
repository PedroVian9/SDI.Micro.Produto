namespace SDI.Back.API.Data;

public static class PostgresConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return FromDatabaseUrl(databaseUrl);
        }

        var host = Environment.GetEnvironmentVariable("POSTGRESQL_HOST");
        var port = Environment.GetEnvironmentVariable("POSTGRESQL_PORT");
        var database = Environment.GetEnvironmentVariable("POSTGRESQL_DATABASE");
        var user = Environment.GetEnvironmentVariable("POSTGRESQL_USER");
        var password = Environment.GetEnvironmentVariable("POSTGRESQL_PASSWORD");

        if (!string.IsNullOrWhiteSpace(host) &&
            !string.IsNullOrWhiteSpace(database) &&
            !string.IsNullOrWhiteSpace(user) &&
            !string.IsNullOrWhiteSpace(password))
        {
            return ApplyDefaults(new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.TryParse(port, out var parsedPort) ? parsedPort : 5432,
                Database = database,
                Username = user,
                Password = password
            }).ConnectionString;
        }

        var configuredConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configuredConnection))
        {
            return ApplyDefaults(new Npgsql.NpgsqlConnectionStringBuilder(configuredConnection)).ConnectionString;
        }

        throw new InvalidOperationException("Configure DATABASE_URL, ConnectionStrings:DefaultConnection ou as variaveis POSTGRESQL_*.");
    }

    private static string FromDatabaseUrl(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("DATABASE_URL invalida.");
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2)
        {
            throw new InvalidOperationException("DATABASE_URL precisa conter usuario e senha.");
        }

        return ApplyDefaults(new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1])
        }).ConnectionString;
    }

    private static Npgsql.NpgsqlConnectionStringBuilder ApplyDefaults(Npgsql.NpgsqlConnectionStringBuilder builder)
    {
        builder.Pooling = true;
        builder.MaxPoolSize = 100;

        var schema = Environment.GetEnvironmentVariable("DB_SCHEMA");
        if (!string.IsNullOrWhiteSpace(schema))
        {
            builder.SearchPath = schema;
        }

        return builder;
    }
}
