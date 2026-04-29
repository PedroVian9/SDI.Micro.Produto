using Dapper;
using SDI.Back.Template.Data;
using SDI.Back.Template.Models.Entity;
using SDI.Back.Template.Models.Responses;
using SDI.Back.Template.Repositories.Interfaces;

namespace SDI.Back.Template.Repositories;

public sealed class TransporteRepository(IDbConnectionFactory connectionFactory) : ITransporteRepository
{
    public async Task<PagedResult<Transporte>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, CancellationToken cancellationToken)
    {
        const string sql = """
            select * from sdi.transporte
            where (@ativo is null or ativo = @ativo)
              and (@busca is null or nome ilike '%' || @busca || '%' or descricao ilike '%' || @busca || '%')
            order by nome
            limit @tamanhoPagina offset @offset;

            select count(1) from sdi.transporte
            where (@ativo is null or ativo = @ativo)
              and (@busca is null or nome ilike '%' || @busca || '%' or descricao ilike '%' || @busca || '%');
            """;

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(sql, new { pagina, tamanhoPagina, offset = (pagina - 1) * tamanhoPagina, ativo, busca }, cancellationToken: cancellationToken));
        var itens = (await multi.ReadAsync<Transporte>()).AsList();
        var total = await multi.ReadSingleAsync<long>();
        return new PagedResult<Transporte> { Itens = itens, Pagina = pagina, TamanhoPagina = tamanhoPagina, Total = total };
    }

    public async Task<Transporte?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "select * from sdi.transporte where id = @id;";
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Transporte>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "select exists(select 1 from sdi.transporte where id = @id and ativo = true);";
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<Transporte> CriarAsync(Transporte transporte, CancellationToken cancellationToken)
    {
        const string sql = """
            insert into sdi.transporte (nome, descricao, usuario_cadastro)
            values (@nome, @descricao, @usuarioCadastro)
            returning *;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Transporte>(new CommandDefinition(sql, transporte, cancellationToken: cancellationToken));
    }

    public async Task<Transporte?> AtualizarAsync(Transporte transporte, CancellationToken cancellationToken)
    {
        const string sql = """
            update sdi.transporte
               set nome = @nome,
                   descricao = @descricao,
                   usuario_alteracao = @usuarioAlteracao
             where id = @id
            returning *;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Transporte>(new CommandDefinition(sql, transporte, cancellationToken: cancellationToken));
    }

    public async Task<bool> DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken)
    {
        const string sql = """
            update sdi.transporte
               set ativo = @ativo,
                   usuario_alteracao = @usuarioAlteracao
             where id = @id;
            """;
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, ativo, usuarioAlteracao }, cancellationToken: cancellationToken));
        return affected > 0;
    }
}
