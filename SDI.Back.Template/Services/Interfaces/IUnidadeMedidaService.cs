using SDI.Back.Template.Models.Dto.Input;
using SDI.Back.Template.Models.Dto.Output;
using SDI.Back.Template.Models.Responses;

namespace SDI.Back.Template.Services.Interfaces;

public interface IUnidadeMedidaService
{
    Task<PagedResult<UnidadeMedidaOutput>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, CancellationToken cancellationToken);
    Task<UnidadeMedidaOutput> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UnidadeMedidaOutput> CriarAsync(UnidadeMedidaInput input, CancellationToken cancellationToken);
    Task<UnidadeMedidaOutput> AtualizarAsync(Guid id, UnidadeMedidaInput input, CancellationToken cancellationToken);
    Task DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken);
}
