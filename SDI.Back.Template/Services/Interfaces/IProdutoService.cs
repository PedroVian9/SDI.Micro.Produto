using SDI.Back.Template.Models.Dto.Input;
using SDI.Back.Template.Models.Dto.Output;
using SDI.Back.Template.Models.Responses;

namespace SDI.Back.Template.Services.Interfaces;

public interface IProdutoService
{
    Task<PagedResult<ProdutoOutput>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, Guid? categoriaId, Guid? transporteId, Guid? unidadeMedidaId, CancellationToken cancellationToken);
    Task<ProdutoOutput> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ProdutoOutput> CriarAsync(ProdutoInput input, CancellationToken cancellationToken);
    Task<ProdutoOutput> AtualizarAsync(Guid id, ProdutoInput input, CancellationToken cancellationToken);
    Task DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken);
}
