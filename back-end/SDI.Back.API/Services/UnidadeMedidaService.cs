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

public sealed class UnidadeMedidaService(IUnidadeMedidaRepository repository, IKafkaEventPublisher kafkaEventPublisher) : IUnidadeMedidaService
{
    public async Task<PagedResult<UnidadeMedidaOutput>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, CancellationToken cancellationToken)
    {
        var (Pagina, TamanhoPagina) = ServiceValidation.NormalizePagination(pagina, tamanhoPagina);
        var result = await repository.ListarAsync(Pagina, TamanhoPagina, ativo, ServiceValidation.Optional(busca, "Busca", 150), cancellationToken);
        return result.MapPage(x => x.ToOutput());
    }

    public async Task<UnidadeMedidaOutput> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new DomainException("Unidade de medida nao encontrada.", StatusCodes.Status404NotFound);
        return entity.ToOutput();
    }

    public async Task<UnidadeMedidaOutput> CriarAsync(UnidadeMedidaInput input, Guid usuarioId, CancellationToken cancellationToken)
    {
        var entity = new UnidadeMedida
        {
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Sigla = ServiceValidation.Required(input.Sigla, "Sigla", 20).ToUpperInvariant(),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 500),
            UsuarioCadastro = usuarioId
        };

        var output = (await repository.CriarAsync(entity, cancellationToken)).ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<UnidadeMedidaOutput>
        {
            EventType = EventTypes.UnidadeMedidaCriada,
            AggregateType = "unidade-medida",
            AggregateId = output.Id,
            UserId = usuarioId,
            Payload = output
        }, cancellationToken);

        return output;
    }

    public async Task<UnidadeMedidaOutput> AtualizarAsync(Guid id, UnidadeMedidaInput input, Guid usuarioId, CancellationToken cancellationToken)
    {
        var entity = new UnidadeMedida
        {
            Id = id,
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Sigla = ServiceValidation.Required(input.Sigla, "Sigla", 20).ToUpperInvariant(),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 500),
            UsuarioAlteracao = usuarioId
        };

        var updated = await repository.AtualizarAsync(entity, cancellationToken)
            ?? throw new DomainException("Unidade de medida nao encontrada.", StatusCodes.Status404NotFound);
        var output = updated.ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<UnidadeMedidaOutput>
        {
            EventType = EventTypes.UnidadeMedidaAtualizada,
            AggregateType = "unidade-medida",
            AggregateId = output.Id,
            UserId = usuarioId,
            Payload = output
        }, cancellationToken);

        return output;
    }

    public async Task DefinirAtivoAsync(Guid id, bool ativo, Guid usuarioId, CancellationToken cancellationToken)
    {
        if (!await repository.DefinirAtivoAsync(id, ativo, usuarioId, cancellationToken))
        {
            throw new DomainException("Unidade de medida nao encontrada.", StatusCodes.Status404NotFound);
        }

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<StatusChangedPayload>
        {
            EventType = EventTypes.UnidadeMedidaStatusAlterado,
            AggregateType = "unidade-medida",
            AggregateId = id,
            UserId = usuarioId,
            Payload = new StatusChangedPayload
            {
                Id = id,
                Ativo = ativo
            }
        }, cancellationToken);
    }
}
