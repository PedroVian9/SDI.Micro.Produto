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

    public async Task<UnidadeMedidaOutput> CriarAsync(UnidadeMedidaInput input, CancellationToken cancellationToken)
    {
        var entity = new UnidadeMedida
        {
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Sigla = ServiceValidation.Required(input.Sigla, "Sigla", 20).ToUpperInvariant(),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 500),
            UsuarioCadastro = input.UsuarioCadastro
        };

        var output = (await repository.CriarAsync(entity, cancellationToken)).ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<UnidadeMedidaOutput>
        {
            EventType = EventTypes.UnidadeMedidaCriada,
            AggregateType = "unidade-medida",
            AggregateId = output.Id,
            UserId = input.UsuarioCadastro,
            Payload = output
        }, cancellationToken);

        return output;
    }

    public async Task<UnidadeMedidaOutput> AtualizarAsync(Guid id, UnidadeMedidaInput input, CancellationToken cancellationToken)
    {
        var entity = new UnidadeMedida
        {
            Id = id,
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Sigla = ServiceValidation.Required(input.Sigla, "Sigla", 20).ToUpperInvariant(),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 500),
            UsuarioAlteracao = input.UsuarioAlteracao
        };

        var updated = await repository.AtualizarAsync(entity, cancellationToken)
            ?? throw new DomainException("Unidade de medida nao encontrada.", StatusCodes.Status404NotFound);
        var output = updated.ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<UnidadeMedidaOutput>
        {
            EventType = EventTypes.UnidadeMedidaAtualizada,
            AggregateType = "unidade-medida",
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
            throw new DomainException("Unidade de medida nao encontrada.", StatusCodes.Status404NotFound);
        }

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<StatusChangedPayload>
        {
            EventType = EventTypes.UnidadeMedidaStatusAlterado,
            AggregateType = "unidade-medida",
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
