namespace SDI.Back.Template.Models.Dto.Output;

public sealed class UnidadeMedidaOutput
{
    public Guid Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Sigla { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public bool Ativo { get; init; }
    public DateTimeOffset DataCadastro { get; init; }
    public DateTimeOffset? UltimaAlteracao { get; init; }
}
