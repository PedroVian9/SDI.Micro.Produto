namespace SDI.Back.API.Models.Entity;

public sealed class Categoria : AuditableEntity
{
    public Guid? CategoriaPaiId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
}
