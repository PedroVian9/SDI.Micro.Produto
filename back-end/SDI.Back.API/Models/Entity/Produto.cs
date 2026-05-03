namespace SDI.Back.API.Models.Entity;

public sealed class Produto : AuditableEntity
{
    public Guid TransporteId { get; init; }
    public Guid CategoriaId { get; init; }
    public Guid UnidadeMedidaId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
}
