using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Ftux.DTOs;
using Tenants.Application.Ftux.Queries.GetTenantFtuxStatus;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pelo diagnóstico e monitoramento da experiência de primeiro acesso (FTUX) da agência.
/// </summary>
[ApiController]
[Route("api/v1/tenants/ftux-status")]
public sealed class TenantFtuxController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantFtuxController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory para envio de consultas.</param>
    public TenantFtuxController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Obtém o status consolidado de progresso dos 4 passos essenciais do onboarding operacional da agência.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Estado do FTUX com percentual e detalhe dos passos.</returns>
    [HttpGet]
    [EndpointSummary("Obtém o progresso consolidado do checklist de primeiro acesso (FTUX) do inquilino")]
    [ProducesResponseType(typeof(Result<TenantFtuxStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TenantFtuxStatusDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<TenantFtuxStatusDto>>> GetFtuxStatus(
        CancellationToken cancellationToken = default)
    {
        var query = new GetTenantFtuxStatusQuery();
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
