namespace SDI.Back.Template.Data;

public static class PostgresConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
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
            return $"Host={host};Port={FirstNonEmpty(port, "5432")};Database={database};Username={user};Password={password};Pooling=true;Maximum Pool Size=100";
        }

        var configuredConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configuredConnection))
        {
            return configuredConnection;
        }

        throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection ou as variaveis POSTGRESQL_*.");
    }

    private static string FirstNonEmpty(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
