using SDI.Back.API.Exceptions;
using SDI.Back.API.Messaging;
using SDI.Back.API.Models.Dto.Input;
using SDI.Back.API.Models.Dto.Output;
using SDI.Back.API.Models.Entity;
using SDI.Back.API.Models.Messaging;
using SDI.Back.API.Models.Responses;
using SDI.Back.API.Repositories.Interfaces;
using SDI.Back.API.Services.Interfaces;

namespace SDI.Back.API.Services;

public sealed class CategoriaService(ICategoriaRepository repository, IKafkaEventPublisher kafkaEventPublisher) : ICategoriaService
{
    public async Task<PagedResult<CategoriaOutput>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, Guid? categoriaPaiId, CancellationToken cancellationToken)
    {
        var (Pagina, TamanhoPagina) = ServiceValidation.NormalizePagination(pagina, tamanhoPagina);
        var result = await repository.ListarAsync(Pagina, TamanhoPagina, ativo, ServiceValidation.Optional(busca, "Busca", 150), categoriaPaiId, cancellationToken);
        return result.MapPage(x => x.ToOutput());
    }

    public async Task<CategoriaOutput> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new DomainException("Categoria nao encontrada.", StatusCodes.Status404NotFound);
        return entity.ToOutput();
    }

    public async Task<CategoriaOutput> CriarAsync(CategoriaInput input, CancellationToken cancellationToken)
    {
        await ValidarCategoriaPaiAsync(null, input.CategoriaPaiId, cancellationToken);

        var entity = new Categoria
        {
            CategoriaPaiId = input.CategoriaPaiId,
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 500),
            UsuarioCadastro = input.UsuarioCadastro
        };

        var output = (await repository.CriarAsync(entity, cancellationToken)).ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<CategoriaOutput>
        {
            EventType = EventTypes.CategoriaCriada,
            AggregateType = "categoria",
            AggregateId = output.Id,
            UserId = input.UsuarioCadastro,
            Payload = output
        }, cancellationToken);

        return output;
    }

    public async Task<CategoriaOutput> AtualizarAsync(Guid id, CategoriaInput input, CancellationToken cancellationToken)
    {
        await ValidarCategoriaPaiAsync(id, input.CategoriaPaiId, cancellationToken);

        var entity = new Categoria
        {
            Id = id,
            CategoriaPaiId = input.CategoriaPaiId,
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 500),
            UsuarioAlteracao = input.UsuarioAlteracao
        };

        var updated = await repository.AtualizarAsync(entity, cancellationToken)
            ?? throw new DomainException("Categoria nao encontrada.", StatusCodes.Status404NotFound);
        var output = updated.ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<CategoriaOutput>
        {
            EventType = EventTypes.CategoriaAtualizada,
            AggregateType = "categoria",
            AggregateId = output.Id,
            UserId = input.UsuarioAlteracao,
            Payload = output
        }, cancellationToken);

        return output;
    }

    public async Task DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken)
    {
        if (!await repository.DefinirAtivoAsync(id, ativo, usuarioAlteracao, cancellationToken))
        {
            throw new DomainException("Categoria nao encontrada.", StatusCodes.Status404NotFound);
        }

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<StatusChangedPayload>
        {
            EventType = EventTypes.CategoriaStatusAlterado,
            AggregateType = "categoria",
            AggregateId = id,
            UserId = usuarioAlteracao,
            Payload = new StatusChangedPayload
            {
                Id = id,
                Ativo = ativo
            }
        }, cancellationToken);
    }

    private async Task ValidarCategoriaPaiAsync(Guid? categoriaId, Guid? categoriaPaiId, CancellationToken cancellationToken)
    {
        if (categoriaPaiId is null)
        {
            return;
        }

        if (categoriaPaiId == categoriaId)
        {
            throw new DomainException("Categoria nao pode ser pai dela mesma.");
        }

        if (!await repository.ExisteAsync(categoriaPaiId.Value, cancellationToken))
        {
            throw new DomainException("Categoria pai nao encontrada ou inativa.");
        }
    }
}
