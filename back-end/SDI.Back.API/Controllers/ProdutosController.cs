using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SDI.Back.API.Models.Dto.Input;
using SDI.Back.API.Models.Dto.Output;
using SDI.Back.API.Models.Responses;
using SDI.Back.API.Services.Interfaces;
using System.Net;

namespace SDI.Back.API.Controllers;

[ApiController]
[Authorize]
[Route("produtos")]
public sealed class ProdutosController(IProdutoService service, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProdutoOutput>>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
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
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.ObterPorIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ProdutoOutput>.Ok(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProdutoOutput>), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Criar([FromBody] ProdutoInput input, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(input, currentUser.RequireUserId(), cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, ApiResponse<ProdutoOutput>.Created(result));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProdutoOutput>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] ProdutoInput input, CancellationToken cancellationToken)
    {
        var result = await service.AtualizarAsync(id, input, currentUser.RequireUserId(), cancellationToken);
        return Ok(ApiResponse<ProdutoOutput>.Ok(result));
    }

    [HttpPatch("{id:guid}/ativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Ativar(Guid id, CancellationToken cancellationToken)
    {
        await service.DefinirAtivoAsync(id, true, currentUser.RequireUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/inativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
    {
        await service.DefinirAtivoAsync(id, false, currentUser.RequireUserId(), cancellationToken);
        return NoContent();
    }
}
