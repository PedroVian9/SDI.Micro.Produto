using Microsoft.AspNetCore.Mvc;
using SDI.Back.Template.Models.Dto.Input;
using SDI.Back.Template.Models.Dto.Output;
using SDI.Back.Template.Models.Responses;
using SDI.Back.Template.Services.Interfaces;
using System.Net;

namespace SDI.Back.Template.Controllers;

[ApiController]
[Route("categorias")]
public sealed class CategoriasController(ICategoriaService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CategoriaOutput>>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20, [FromQuery] bool? ativo = true, [FromQuery] string? busca = null, [FromQuery] Guid? categoriaPaiId = null, CancellationToken cancellationToken = default)
    {
        var result = await service.ListarAsync(pagina, tamanhoPagina, ativo, busca, categoriaPaiId, cancellationToken);
        return Ok(ApiResponse<PagedResult<CategoriaOutput>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoriaOutput>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.ObterPorIdAsync(id, cancellationToken);
        return Ok(ApiResponse<CategoriaOutput>.Ok(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoriaOutput>), (int)HttpStatusCode.Created)]
    public async Task<IActionResult> Criar([FromBody] CategoriaInput input, CancellationToken cancellationToken)
    {
        var result = await service.CriarAsync(input, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = result.Id }, ApiResponse<CategoriaOutput>.Created(result));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoriaOutput>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] CategoriaInput input, CancellationToken cancellationToken)
    {
        var result = await service.AtualizarAsync(id, input, cancellationToken);
        return Ok(ApiResponse<CategoriaOutput>.Ok(result));
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
