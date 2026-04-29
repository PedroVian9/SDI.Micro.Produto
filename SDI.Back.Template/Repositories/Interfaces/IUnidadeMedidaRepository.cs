using SDI.Back.Template.Models.Entity;
using SDI.Back.Template.Models.Responses;

namespace SDI.Back.Template.Repositories.Interfaces;

public interface IUnidadeMedidaRepository
{
    Task<PagedResult<UnidadeMedida>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, CancellationToken cancellationToken);
    Task<UnidadeMedida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken);
    Task<UnidadeMedida> CriarAsync(UnidadeMedida unidadeMedida, CancellationToken cancellationToken);
    Task<UnidadeMedida?> AtualizarAsync(UnidadeMedida unidadeMedida, CancellationToken cancellationToken);
    Task<bool> DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken);
}
