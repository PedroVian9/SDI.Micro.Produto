using SDI.Back.Template.Models.Dto.Output;
using SDI.Back.Template.Models.Entity;
using SDI.Back.Template.Models.Responses;

namespace SDI.Back.Template.Services;

internal static class MappingExtensions
{
    public static PagedResult<TOutput> MapPage<TEntity, TOutput>(this PagedResult<TEntity> page, Func<TEntity, TOutput> mapper)
    {
        return new PagedResult<TOutput>
        {
            Itens = [.. page.Itens.Select(mapper)],
            Pagina = page.Pagina,
            TamanhoPagina = page.TamanhoPagina,
            Total = page.Total
        };
    }

    public static TransporteOutput ToOutput(this Transporte entity)
    {
        return new TransporteOutput
        {
            Id = entity.Id,
            Nome = entity.Nome,
            Descricao = entity.Descricao,
            Ativo = entity.Ativo,
            DataCadastro = entity.DataCadastro,
            UltimaAlteracao = entity.UltimaAlteracao
        };
    }

    public static CategoriaOutput ToOutput(this Categoria entity)
    {
        return new CategoriaOutput
        {
            Id = entity.Id,
            CategoriaPaiId = entity.CategoriaPaiId,
            Nome = entity.Nome,
            Descricao = entity.Descricao,
            Ativo = entity.Ativo,
            DataCadastro = entity.DataCadastro,
            UltimaAlteracao = entity.UltimaAlteracao
        };
    }

    public static UnidadeMedidaOutput ToOutput(this UnidadeMedida entity)
    {
        return new UnidadeMedidaOutput
        {
            Id = entity.Id,
            Nome = entity.Nome,
            Sigla = entity.Sigla,
            Descricao = entity.Descricao,
            Ativo = entity.Ativo,
            DataCadastro = entity.DataCadastro,
            UltimaAlteracao = entity.UltimaAlteracao
        };
    }

    public static ProdutoOutput ToOutput(this Produto entity)
    {
        return new ProdutoOutput
        {
            Id = entity.Id,
            TransporteId = entity.TransporteId,
            CategoriaId = entity.CategoriaId,
            UnidadeMedidaId = entity.UnidadeMedidaId,
            Codigo = entity.Codigo,
            Nome = entity.Nome,
            Descricao = entity.Descricao,
            Ativo = entity.Ativo,
            DataCadastro = entity.DataCadastro,
            UltimaAlteracao = entity.UltimaAlteracao
        };
    }
}
