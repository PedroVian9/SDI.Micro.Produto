namespace SDI.Back.Template.Models.Dto.Input;

public sealed class ProdutoInput
{
    public Guid TransporteId { get; init; }
    public Guid CategoriaId { get; init; }
    public Guid UnidadeMedidaId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public Guid? UsuarioCadastro { get; init; }
    public Guid? UsuarioAlteracao { get; init; }
}
