namespace SDI.Back.Template.Models.Entity;

public sealed class Transporte : AuditableEntity
{
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
}
