using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Meta;
using Integrations.Domain.Meta;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

/// <summary>Insights detalhados da Meta, com contexto de workspace e conta interna.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
[Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Tenant")]
[ApiController]
[Route("api/v1/integrations/meta/insights")]
public sealed class MetaInsightsController(ISender sender) : ControllerBase
{
    /// <summary>Consulta métricas diárias em nível de conta, campanha, conjunto ou anúncio.</summary>
    /// <param name="workspaceId">Workspace autorizado.</param>
    /// <param name="accountId">ID interno da conta vinculada.</param>
    /// <param name="filter">Filtros oficiais de consulta.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Linhas sem agregação de métricas não aditivas.</returns>
    [HttpGet]
    [EndpointSummary("Consulta Insights Meta com evento de conversão explícito e métricas opcionais")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<MetaInsight>>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Get([FromQuery] Guid workspaceId, [FromQuery] Guid accountId,
        [FromQuery] MetaInsightsFilter filter, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMetaInsightsQuery(workspaceId, accountId, filter), cancellationToken);
        return result.IsSuccess ? Ok(result) : StatusCode(result.Error.Type switch
        { ErrorType.Unauthorized => 401, ErrorType.Forbidden => 403, ErrorType.NotFound => 404, ErrorType.Validation => 422, _ => 502 }, result);
    }
}
