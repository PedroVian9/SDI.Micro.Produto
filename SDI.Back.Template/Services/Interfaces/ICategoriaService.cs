using SDI.Back.Template.Models.Dto.Input;
using SDI.Back.Template.Models.Dto.Output;
using SDI.Back.Template.Models.Responses;

namespace SDI.Back.Template.Services.Interfaces;

public interface ICategoriaService
{
    Task<PagedResult<CategoriaOutput>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, Guid? categoriaPaiId, CancellationToken cancellationToken);
    Task<CategoriaOutput> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoriaOutput> CriarAsync(CategoriaInput input, CancellationToken cancellationToken);
    Task<CategoriaOutput> AtualizarAsync(Guid id, CategoriaInput input, CancellationToken cancellationToken);
    Task DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken);
}
