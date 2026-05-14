using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SDI.Back.API.Models.Dto.Output;
using SDI.Back.API.Models.Responses;
using System.Net;

namespace SDI.Back.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("saude")]
public class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet("/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME");
        return Ok(new
        {
            status = "ok",
            service = string.IsNullOrWhiteSpace(serviceName) ? "produtos-service" : serviceName
        });
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckResponse>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckResponse>), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<IActionResult> GetDetailed()
    {
        var report = await healthCheckService.CheckHealthAsync();
        
        var responseDto = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration.ToString(),
            Entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new HealthCheckEntryResponse
                {
                    Status = e.Value.Status.ToString(),
                    Description = e.Value.Description ?? string.Empty,
                    Duration = e.Value.Duration.ToString(),
                    Data = e.Value.Data
                })
        };

        var retorno = report.Status == HealthStatus.Healthy
            ? ApiResponse<HealthCheckResponse>.Ok(responseDto, "Sistema operando normalmente.")
            : ApiResponse<HealthCheckResponse>.Fail("Sistema com instabilidade.", (int)HttpStatusCode.ServiceUnavailable);

        return report.Status == HealthStatus.Healthy ? Ok(retorno) : StatusCode((int)HttpStatusCode.ServiceUnavailable, retorno);
    }
}
