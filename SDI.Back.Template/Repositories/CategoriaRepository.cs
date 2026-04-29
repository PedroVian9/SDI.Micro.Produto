using Dapper;
using SDI.Back.Template.Data;
using SDI.Back.Template.Models.Entity;
using SDI.Back.Template.Models.Responses;
using SDI.Back.Template.Repositories.Interfaces;

namespace SDI.Back.Template.Repositories;

public sealed class CategoriaRepository(IDbConnectionFactory connectionFactory) : ICategoriaRepository
{
    public async Task<PagedResult<Categoria>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, Guid? categoriaPaiId, CancellationToken cancellationToken)
    {
        const string sql = """
            select * from sdi.categoria
            where (@ativo is null or ativo = @ativo)
              and (@categoriaPaiId is null or categoria_pai_id = @categoriaPaiId)
              and (@busca is null or nome ilike '%' || @busca || '%' or descricao ilike '%' || @busca || '%')
            order by nome
            limit @tamanhoPagina offset @offset;

            select count(1) from sdi.categoria
            where (@ativo is null or ativo = @ativo)
              and (@categoriaPaiId is null or categoria_pai_id = @categoriaPaiId)
              and (@busca is null or nome ilike '%' || @busca || '%' or descricao ilike '%' || @busca || '%');
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { pagina, tamanhoPagina, offset = (pagina - 1) * tamanhoPagina, ativo, busca, categoriaPaiId }, cancellationToken: cancellationToken));
        var itens = (await multi.ReadAsync<Categoria>()).AsList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<Categoria> { Itens = itens, Pagina = pagina, TamanhoPagina = tamanhoPagina, Total = total };
    }

    public async Task<Categoria?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "select * from sdi.categoria where id = @id;";
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Categoria>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "select exists(select 1 from sdi.categoria where id = @id and ativo = true);";
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<Categoria> CriarAsync(Categoria categoria, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into sdi.categoria (categoria_pai_id, nome, descricao, usuario_cadastro)
            values (@categoriaPaiId, @nome, @descricao, @usuarioCadastro)
            returning *;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Categoria>(new CommandDefinition(sql, categoria, cancellationToken: cancellationToken));
    }

    public async Task<Categoria?> AtualizarAsync(Categoria categoria, CancellationToken cancellationToken)
    {
        const string sql = """
            update sdi.categoria
               set categoria_pai_id = @categoriaPaiId,
                   nome = @nome,
                   descricao = @descricao,
                   usuario_alteracao = @usuarioAlteracao
             where id = @id
            returning *;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Categoria>(new CommandDefinition(sql, categoria, cancellationToken: cancellationToken));
    }

    public async Task<bool> DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken)
    {
        const string sql = """
            update sdi.categoria
               set ativo = @ativo,
                   usuario_alteracao = @usuarioAlteracao
             where id = @id;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, ativo, usuarioAlteracao }, cancellationToken: cancellationToken));
        return affected > 0;
    }
}
