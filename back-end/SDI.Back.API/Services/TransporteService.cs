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

public sealed class TransporteService(ITransporteRepository repository, IKafkaEventPublisher kafkaEventPublisher) : ITransporteService
{
    public async Task<PagedResult<TransporteOutput>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, CancellationToken cancellationToken)
    {
        var (Pagina, TamanhoPagina) = ServiceValidation.NormalizePagination(pagina, tamanhoPagina);
        var result = await repository.ListarAsync(Pagina, TamanhoPagina, ativo, ServiceValidation.Optional(busca, "Busca", 150), cancellationToken);
        return result.MapPage(x => x.ToOutput());
    }

    public async Task<TransporteOutput> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new DomainException("Transporte nao encontrado.", StatusCodes.Status404NotFound);
        return entity.ToOutput();
    }

    public async Task<TransporteOutput> CriarAsync(TransporteInput input, Guid usuarioId, CancellationToken cancellationToken)
    {
        var entity = new Transporte
        {
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 500),
            UsuarioCadastro = usuarioId
        };

        var output = (await repository.CriarAsync(entity, cancellationToken)).ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<TransporteOutput>
        {
            EventType = EventTypes.TransporteCriado,
            AggregateType = "transporte",
            AggregateId = output.Id,
            UserId = usuarioId,
            Payload = output
        }, cancellationToken);

        return output;
    }

    public async Task<TransporteOutput> AtualizarAsync(Guid id, TransporteInput input, Guid usuarioId, CancellationToken cancellationToken)
    {
        var entity = new Transporte
        {
            Id = id,
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 500),
            UsuarioAlteracao = usuarioId
        };

        var updated = await repository.AtualizarAsync(entity, cancellationToken)
            ?? throw new DomainException("Transporte nao encontrado.", StatusCodes.Status404NotFound);
        var output = updated.ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<TransporteOutput>
        {
            EventType = EventTypes.TransporteAtualizado,
            AggregateType = "transporte",
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
            throw new DomainException("Transporte nao encontrado.", StatusCodes.Status404NotFound);
        }

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<StatusChangedPayload>
        {
            EventType = EventTypes.TransporteStatusAlterado,
            AggregateType = "transporte",
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
