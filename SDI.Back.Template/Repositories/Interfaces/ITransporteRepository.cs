using SDI.Back.Template.Models.Entity;
using SDI.Back.Template.Models.Responses;

namespace SDI.Back.Template.Repositories.Interfaces;

public interface ITransporteRepository
{
    Task<PagedResult<Transporte>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, CancellationToken cancellationToken);
    Task<Transporte?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken);
    Task<Transporte> CriarAsync(Transporte transporte, CancellationToken cancellationToken);
    Task<Transporte?> AtualizarAsync(Transporte transporte, CancellationToken cancellationToken);
    Task<bool> DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken);
}
