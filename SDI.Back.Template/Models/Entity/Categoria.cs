namespace SDI.Back.Template.Models.Entity;

public sealed class Categoria : AuditableEntity
{
    public Guid? CategoriaPaiId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
}
