namespace SDI.Back.API.Models.Entity;

public sealed class UnidadeMedida : AuditableEntity
{
    public string Nome { get; init; } = string.Empty;
    public string Sigla { get; init; } = string.Empty;
    public string? Descricao { get; init; }
}
