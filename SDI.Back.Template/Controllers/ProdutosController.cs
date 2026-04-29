using Microsoft.AspNetCore.Mvc;
using SDI.Back.Template.Models.Dto.Input;
using SDI.Back.Template.Models.Dto.Output;
using SDI.Back.Template.Models.Responses;
using SDI.Back.Template.Services.Interfaces;
using System.Net;

namespace SDI.Back.Template.Controllers;

[ApiController]
[Route("produtos")]
public sealed class ProdutosController(IProdutoService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProdutoOutput>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        [FromQuery] bool? ativo = true,
        [FromQuery] string? busca = null,
        [FromQuery] Guid? categoriaId = null,
        [FromQuery] Guid? transporteId = null,
        [FromQuery] Guid? unidadeMedidaId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListarAsync(pagina, tamanhoPagina, ativo, busca, categoriaId, transporteId, unidadeMedidaId, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProdutoOutput>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProdutoOutput>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.ObterPorIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ProdutoOutput>.Ok(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProdutoOutput>), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> Criar([FromBody] ProdutoInput input, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(input, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, ApiResponse<ProdutoOutput>.Created(result));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProdutoOutput>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] ProdutoInput input, CancellationToken cancellationToken)
    {
        var result = await service.AtualizarAsync(id, input, cancellationToken);
        return Ok(ApiResponse<ProdutoOutput>.Ok(result));
    }

    [HttpPatch("{id:guid}/ativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Ativar(Guid id, [FromQuery] Guid? usuarioAlteracao, CancellationToken cancellationToken)
    {
        await service.DefinirAtivoAsync(id, true, usuarioAlteracao, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/inativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Inativar(Guid id, [FromQuery] Guid? usuarioAlteracao, CancellationToken cancellationToken)
    {
        await service.DefinirAtivoAsync(id, false, usuarioAlteracao, cancellationToken);
        return NoContent();
    }
}
