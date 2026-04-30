using Dapper;
using SDI.Back.Template.Data;
using SDI.Back.Template.Models.Entity;
using SDI.Back.Template.Models.Responses;
using SDI.Back.Template.Repositories.Interfaces;

namespace SDI.Back.Template.Repositories;

public sealed class ProdutoRepository(IDbConnectionFactory connectionFactory) : IProdutoRepository
{
    public async Task<PagedResult<Produto>> ListarAsync(
        int pagina,
        int tamanhoPagina,
        bool? ativo,
        string? busca,
        Guid? categoriaId,
        Guid? transporteId,
        Guid? unidadeMedidaId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select * from sdi.produto
            where (@ativo is null or ativo = @ativo)
              and (@categoriaId is null or categoria_id = @categoriaId)
              and (@transporteId is null or transporte_id = @transporteId)
              and (@unidadeMedidaId is null or unidade_medida_id = @unidadeMedidaId)
              and (@busca is null or codigo ilike '%' || @busca || '%' or nome ilike '%' || @busca || '%' or descricao ilike '%' || @busca || '%')
            order by nome
            limit @tamanhoPagina offset @offset;

            select count(1) from sdi.produto
            where (@ativo is null or ativo = @ativo)
              and (@categoriaId is null or categoria_id = @categoriaId)
              and (@transporteId is null or transporte_id = @transporteId)
              and (@unidadeMedidaId is null or unidade_medida_id = @unidadeMedidaId)
              and (@busca is null or codigo ilike '%' || @busca || '%' or nome ilike '%' || @busca || '%' or descricao ilike '%' || @busca || '%');
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            sql,
            new
            {
                pagina,
                tamanhoPagina,
                offset = (pagina - 1) * tamanhoPagina,
                ativo,
                busca,
                categoriaId,
                transporteId,
                unidadeMedidaId
            },
            cancellationToken: cancellationToken));

        var itens = (await multi.ReadAsync<Produto>()).AsList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<Produto> { Itens = itens, Pagina = pagina, TamanhoPagina = tamanhoPagina, Total = total };
    }

    public async Task<Produto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "select * from sdi.produto where id = @id;";
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Produto>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<Produto> CriarAsync(Produto produto, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into sdi.produto (
                transporte_id,
                categoria_id,
                unidade_medida_id,
                codigo,
                nome,
                descricao,
                usuario_cadastro)
            values (
                @transporteId,
                @categoriaId,
                @unidadeMedidaId,
                @codigo,
                @nome,
                @descricao,
                @usuarioCadastro)
            returning *;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Produto>(new CommandDefinition(sql, produto, cancellationToken: cancellationToken));
    }

    public async Task<Produto?> AtualizarAsync(Produto produto, CancellationToken cancellationToken)
    {
        const string sql = """
            update sdi.produto
               set transporte_id = @transporteId,
                   categoria_id = @categoriaId,
                   unidade_medida_id = @unidadeMedidaId,
                   codigo = @codigo,
                   nome = @nome,
                   descricao = @descricao,
                   usuario_alteracao = @usuarioAlteracao
             where id = @id
            returning *;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Produto>(new CommandDefinition(sql, produto, cancellationToken: cancellationToken));
    }

    public async Task<bool> DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken)
    {
        const string sql = """
            update sdi.produto
               set ativo = @ativo,
                   usuario_alteracao = @usuarioAlteracao
             where id = @id;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, ativo, usuarioAlteracao }, cancellationToken: cancellationToken));
        return affected > 0;
    }
}
