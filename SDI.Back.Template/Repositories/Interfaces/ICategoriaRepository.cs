using SDI.Back.Template.Models.Entity;
using SDI.Back.Template.Models.Responses;

namespace SDI.Back.Template.Repositories.Interfaces;

public interface ICategoriaRepository
{
    Task<PagedResult<Categoria>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, Guid? categoriaPaiId, CancellationToken cancellationToken);
    Task<Categoria?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken);
    Task<Categoria> CriarAsync(Categoria categoria, CancellationToken cancellationToken);
    Task<Categoria?> AtualizarAsync(Categoria categoria, CancellationToken cancellationToken);
    Task<bool> DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken);
}
