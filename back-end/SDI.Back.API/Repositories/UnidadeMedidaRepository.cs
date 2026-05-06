using Dapper;
using SDI.Back.API.Data;
using SDI.Back.API.Models.Entity;
using SDI.Back.API.Models.Responses;
using SDI.Back.API.Repositories.Interfaces;

namespace SDI.Back.API.Repositories;

public sealed class UnidadeMedidaRepository(IDbConnectionFactory connectionFactory) : IUnidadeMedidaRepository
{
    public async Task<PagedResult<UnidadeMedida>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, CancellationToken cancellationToken)
    {
        var sql = $"""
            select * from {DatabaseIdentifiers.UnidadeMedida}
            where (@ativo is null or ativo = @ativo)
              and (@busca is null or nome ilike '%' || @busca || '%' or sigla ilike '%' || @busca || '%' or descricao ilike '%' || @busca || '%')
            order by nome
            limit @tamanhoPagina offset @offset;

            select count(1) from {DatabaseIdentifiers.UnidadeMedida}
            where (@ativo is null or ativo = @ativo)
              and (@busca is null or nome ilike '%' || @busca || '%' or sigla ilike '%' || @busca || '%' or descricao ilike '%' || @busca || '%');
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { pagina, tamanhoPagina, offset = (pagina - 1) * tamanhoPagina, ativo, busca }, cancellationToken: cancellationToken));
        var itens = (await multi.ReadAsync<UnidadeMedida>()).AsList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<UnidadeMedida> { Itens = itens, Pagina = pagina, TamanhoPagina = tamanhoPagina, Total = total };
    }

    public async Task<UnidadeMedida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var sql = $"select * from {DatabaseIdentifiers.UnidadeMedida} where id = @id;";
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<UnidadeMedida>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken)
    {
        var sql = $"select exists(select 1 from {DatabaseIdentifiers.UnidadeMedida} where id = @id and ativo = true);";
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<UnidadeMedida> CriarAsync(UnidadeMedida unidadeMedida, CancellationToken cancellationToken)
    {
        var sql = $"""
            insert into {DatabaseIdentifiers.UnidadeMedida} (nome, sigla, descricao, usuario_cadastro)
            values (@nome, @sigla, @descricao, @usuarioCadastro)
            returning *;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<UnidadeMedida>(new CommandDefinition(sql, unidadeMedida, cancellationToken: cancellationToken));
    }

    public async Task<UnidadeMedida?> AtualizarAsync(UnidadeMedida unidadeMedida, CancellationToken cancellationToken)
    {
        var sql = $"""
            update {DatabaseIdentifiers.UnidadeMedida}
               set nome = @nome,
                   sigla = @sigla,
                   descricao = @descricao,
                   usuario_alteracao = @usuarioAlteracao
             where id = @id
            returning *;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<UnidadeMedida>(new CommandDefinition(sql, unidadeMedida, cancellationToken: cancellationToken));
    }

    public async Task<bool> DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken)
    {
        var sql = $"""
            update {DatabaseIdentifiers.UnidadeMedida}
               set ativo = @ativo,
                   usuario_alteracao = @usuarioAlteracao
             where id = @id;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, ativo, usuarioAlteracao }, cancellationToken: cancellationToken));
        return affected > 0;
    }
}
