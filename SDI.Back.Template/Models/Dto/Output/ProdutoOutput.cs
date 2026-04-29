namespace SDI.Back.Template.Models.Dto.Output;

public sealed class ProdutoOutput
{
    public Guid Id { get; init; }
    public Guid TransporteId { get; init; }
    public Guid CategoriaId { get; init; }
    public Guid UnidadeMedidaId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public decimal QuantidadeTotal { get; init; }
    public bool Ativo { get; init; }
    public DateTimeOffset DataCadastro { get; init; }
    public DateTimeOffset? UltimaAlteracao { get; init; }
}
