using SDI.Back.Template.Models.Entity;
using SDI.Back.Template.Models.Responses;

namespace SDI.Back.Template.Repositories.Interfaces;

public interface IProdutoRepository
{
    Task<PagedResult<Produto>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, Guid? categoriaId, Guid? transporteId, Guid? unidadeMedidaId, CancellationToken cancellationToken);
    Task<Produto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Produto> CriarAsync(Produto produto, CancellationToken cancellationToken);
    Task<Produto?> AtualizarAsync(Produto produto, CancellationToken cancellationToken);
    Task<bool> DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken);
}
