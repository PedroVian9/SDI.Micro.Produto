using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SDI.Back.Template.Models.Dto.Output;
using SDI.Back.Template.Models.Responses;
using System.Net;

namespace SDI.Back.Template.Controllers;

[ApiController]
[Route("saude")]
public class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckResponse>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ApiResponse<HealthCheckResponse>), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<IActionResult> Get()
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
