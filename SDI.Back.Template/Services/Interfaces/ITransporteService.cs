using SDI.Back.Template.Models.Dto.Input;
using SDI.Back.Template.Models.Dto.Output;
using SDI.Back.Template.Models.Responses;

namespace SDI.Back.Template.Services.Interfaces;

public interface ITransporteService
{
    Task<PagedResult<TransporteOutput>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, CancellationToken cancellationToken);
    Task<TransporteOutput> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TransporteOutput> CriarAsync(TransporteInput input, CancellationToken cancellationToken);
    Task<TransporteOutput> AtualizarAsync(Guid id, TransporteInput input, CancellationToken cancellationToken);
    Task DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken);
}
