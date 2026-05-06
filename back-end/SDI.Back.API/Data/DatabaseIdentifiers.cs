namespace SDI.Back.API.Data;

public static class DatabaseIdentifiers
{
    public static string Transporte => Qualify("produtos_transporte");
    public static string Categoria => Qualify("produtos_categoria");
    public static string UnidadeMedida => Qualify("produtos_unidade_medida");
    public static string Produto => Qualify("produtos_produto");

    private static string Qualify(string tableName)
    {
        var schema = Environment.GetEnvironmentVariable("DB_SCHEMA");
        return $"{QuoteIdentifier(string.IsNullOrWhiteSpace(schema) ? "portal_b2b" : schema)}.{QuoteIdentifier(tableName)}";
    }

    private static string QuoteIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
        {
            throw new InvalidOperationException($"Identificador de banco invalido: {value}");
        }

        return $"\"{value}\"";
    }
}
