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

public sealed class ProdutoService(IProdutoRepository produtoRepository,
                                   ITransporteRepository transporteRepository,
                                   ICategoriaRepository categoriaRepository,
                                   IUnidadeMedidaRepository unidadeMedidaRepository,
                                   IKafkaEventPublisher kafkaEventPublisher) : IProdutoService
{
    public async Task<PagedResult<ProdutoOutput>> ListarAsync(int pagina, int tamanhoPagina, bool? ativo, string? busca, Guid? categoriaId, Guid? transporteId, Guid? unidadeMedidaId, CancellationToken cancellationToken)
    {
        var (Pagina, TamanhoPagina) = ServiceValidation.NormalizePagination(pagina, tamanhoPagina);
        var result = await produtoRepository.ListarAsync(
            Pagina,
            TamanhoPagina,
            ativo,
            ServiceValidation.Optional(busca, "Busca", 150),
            categoriaId,
            transporteId,
            unidadeMedidaId,
            cancellationToken);

        return result.MapPage(x => x.ToOutput());
    }

    public async Task<ProdutoOutput> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await produtoRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new DomainException("Produto nao encontrado.", StatusCodes.Status404NotFound);
        return entity.ToOutput();
    }

    public async Task<ProdutoOutput> CriarAsync(ProdutoInput input, CancellationToken cancellationToken)
    {
        await ValidarRelacionamentosAsync(input, cancellationToken);

        var entity = ToEntity(Guid.Empty, input, isCreate: true);
        var output = (await produtoRepository.CriarAsync(entity, cancellationToken)).ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<ProdutoOutput>
        {
            EventType = EventTypes.ProdutoCriado,
            AggregateType = "produto",
            AggregateId = output.Id,
            UserId = input.UsuarioCadastro,
            Payload = output
        }, cancellationToken);

        return output;
    }

    public async Task<ProdutoOutput> AtualizarAsync(Guid id, ProdutoInput input, CancellationToken cancellationToken)
    {
        await ValidarRelacionamentosAsync(input, cancellationToken);

        var entity = ToEntity(id, input, isCreate: false);
        var updated = await produtoRepository.AtualizarAsync(entity, cancellationToken)
            ?? throw new DomainException("Produto nao encontrado.", StatusCodes.Status404NotFound);
        var output = updated.ToOutput();

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<ProdutoOutput>
        {
            EventType = EventTypes.ProdutoAtualizado,
            AggregateType = "produto",
            AggregateId = output.Id,
            UserId = input.UsuarioAlteracao,
            Payload = output
        }, cancellationToken);

        return output;
    }

    public async Task DefinirAtivoAsync(Guid id, bool ativo, Guid? usuarioAlteracao, CancellationToken cancellationToken)
    {
        if (!await produtoRepository.DefinirAtivoAsync(id, ativo, usuarioAlteracao, cancellationToken))
        {
            throw new DomainException("Produto nao encontrado.", StatusCodes.Status404NotFound);
        }

        await kafkaEventPublisher.PublishAsync(new IntegrationEvent<StatusChangedPayload>
        {
            EventType = EventTypes.ProdutoStatusAlterado,
            AggregateType = "produto",
            AggregateId = id,
            UserId = usuarioAlteracao,
            Payload = new StatusChangedPayload
            {
                Id = id,
                Ativo = ativo
            }
        }, cancellationToken);
    }

    private async Task ValidarRelacionamentosAsync(ProdutoInput input, CancellationToken cancellationToken)
    {
        if (!await transporteRepository.ExisteAsync(input.TransporteId, cancellationToken))
        {
            throw new DomainException("Transporte nao encontrado ou inativo.");
        }

        if (!await categoriaRepository.ExisteAsync(input.CategoriaId, cancellationToken))
        {
            throw new DomainException("Categoria nao encontrada ou inativa.");
        }

        if (!await unidadeMedidaRepository.ExisteAsync(input.UnidadeMedidaId, cancellationToken))
        {
            throw new DomainException("Unidade de medida nao encontrada ou inativa.");
        }

        if (input.QuantidadeTotal < 0)
        {
            throw new DomainException("Quantidade total nao pode ser negativa.");
        }
    }

    private static Produto ToEntity(Guid id, ProdutoInput input, bool isCreate)
    {
        return new Produto
        {
            Id = id,
            TransporteId = input.TransporteId,
            CategoriaId = input.CategoriaId,
            UnidadeMedidaId = input.UnidadeMedidaId,
            Codigo = ServiceValidation.Required(input.Codigo, "Codigo", 60).ToUpperInvariant(),
            Nome = ServiceValidation.Required(input.Nome, "Nome", 150),
            Descricao = ServiceValidation.Optional(input.Descricao, "Descricao", 1000),
            QuantidadeTotal = input.QuantidadeTotal,
            UsuarioCadastro = isCreate ? input.UsuarioCadastro : null,
            UsuarioAlteracao = isCreate ? null : input.UsuarioAlteracao
        };
    }
}
