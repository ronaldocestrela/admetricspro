using BuildingBlocks.Domain.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável por expor endpoints de diagnóstico e integridade da aplicação.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="HealthController"/>.
    /// </summary>
    /// <param name="environment">Ambiente de hospedagem da aplicação.</param>
    public HealthController(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Executa a verificação de saúde operacional da API.
    /// </summary>
    /// <returns>Retorna envelope Result contendo o estado operacional do serviço.</returns>
    [HttpGet]
    [EndpointSummary("Verifica a saúde operacional da API do AdMetricsPro")]
    [ProducesResponseType(typeof(Result<HealthStatusResponse>), StatusCodes.Status200OK)]
    public ActionResult<Result<HealthStatusResponse>> GetHealth()
    {
        var response = new HealthStatusResponse(
            Status: "Healthy",
            TimestampUtc: DateTime.UtcNow,
            Service: "AdMetricsPro API",
            Environment: _environment.EnvironmentName);

        return Ok(Result<HealthStatusResponse>.Success(response));
    }
}
