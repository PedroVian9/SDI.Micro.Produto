using SDI.Back.Template.Exceptions;
using SDI.Back.Template.Messaging;
using SDI.Back.Template.Models.Dto.Input;
using SDI.Back.Template.Models.Dto.Output;
using SDI.Back.Template.Models.Entity;
using SDI.Back.Template.Models.Messaging;
using SDI.Back.Template.Models.Responses;
using SDI.Back.Template.Repositories.Interfaces;
using SDI.Back.Template.Services.Interfaces;

namespace SDI.Back.Template.Services;

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

    public async Task<TransporteOutput> CriarAsync(TransporteInput input, CancellationToken cancellationToken)
    {
        var entity = new Transporte
        {
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 500),
            UsuarioCadastro = input.UsuarioCadastro
        };

        var output = (await repository.CriarAsync(entity, cancellationToken)).ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<TransporteOutput>
        {
            EventType = EventTypes.TransporteCriado,
            AggregateType = "transporte",
            AggregateId = output.Id,
            UserId = input.UsuarioCadastro,
            Payload = output
        }, cancellationToken);

        return output;
    }

    public async Task<TransporteOutput> AtualizarAsync(Guid id, TransporteInput input, CancellationToken cancellationToken)
    {
        var entity = new Transporte
        {
            Id = id,
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 500),
            UsuarioAlteracao = input.UsuarioAlteracao
        };

        var updated = await repository.AtualizarAsync(entity, cancellationToken)
            ?? throw new DomainException("Transporte nao encontrado.", StatusCodes.Status404NotFound);
        var output = updated.ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<TransporteOutput>
        {
            EventType = EventTypes.TransporteAtualizado,
            AggregateType = "transporte",
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
            throw new DomainException("Transporte nao encontrado.", StatusCodes.Status404NotFound);
        }

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<StatusChangedPayload>
        {
            EventType = EventTypes.TransporteStatusAlterado,
            AggregateType = "transporte",
            AggregateId = id,
            UserId = usuarioAlteracao,
            Payload = new StatusChangedPayload
            {
                Id = id,
                Ativo = ativo
            }
        }, cancellationToken);
    }
}
