namespace SDI.Back.Template.Models.Entity;

public sealed class UnidadeMedida : AuditableEntity
{
    public string Nome { get; init; } = string.Empty;
    public string Sigla { get; init; } = string.Empty;
    public string? Descricao { get; init; }
}
