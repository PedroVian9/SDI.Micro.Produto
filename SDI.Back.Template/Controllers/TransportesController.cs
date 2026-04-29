using Microsoft.AspNetCore.Mvc;
using SDI.Back.Template.Models.Dto.Input;
using SDI.Back.Template.Models.Dto.Output;
using SDI.Back.Template.Models.Responses;
using SDI.Back.Template.Services.Interfaces;
using System.Net;

namespace SDI.Back.Template.Controllers;

[ApiController]
[Route("transportes")]
public sealed class TransportesController(ITransporteService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TransporteOutput>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20, [FromQuery] bool? ativo = true, [FromQuery] string? busca = null, CancellationToken cancellationToken = default)
    {
        var result = await service.ListarAsync(pagina, tamanhoPagina, ativo, busca, cancellationToken);
        return Ok(ApiResponse<PagedResult<TransporteOutput>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TransporteOutput>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.ObterPorIdAsync(id, cancellationToken);
        return Ok(ApiResponse<TransporteOutput>.Ok(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransporteOutput>), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> Criar([FromBody] TransporteInput input, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(input, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, ApiResponse<TransporteOutput>.Created(result));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TransporteOutput>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] TransporteInput input, CancellationToken cancellationToken)
    {
        var result = await service.AtualizarAsync(id, input, cancellationToken);
        return Ok(ApiResponse<TransporteOutput>.Ok(result));
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
