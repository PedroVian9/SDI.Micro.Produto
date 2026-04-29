namespace SDI.Back.Template.Models.Dto.Input;

public sealed class UnidadeMedidaInput
{
    public string Nome { get; init; } = string.Empty;
    public string Sigla { get; init; } = string.Empty;
    public string? Descricao { get; init; }
    public Guid? UsuarioCadastro { get; init; }
    public Guid? UsuarioAlteracao { get; init; }
}
