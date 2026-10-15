using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Meta;
using Integrations.Domain.Meta;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

/// <summary>Descoberta, seleção e desvinculação de contas Meta por workspace.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
[Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Tenant")]
[ApiController]
[Route("api/v1/integrations/meta/ad-accounts")]
public sealed class MetaAdAccountsController(ISender sender) : ControllerBase
{
    /// <summary>Lista contas acessíveis pela autorização Meta do workspace.</summary>
    /// <param name="workspaceId">Workspace autorizado.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Contas sem credenciais.</returns>
    [HttpGet]
    [EndpointSummary("Lista contas de anúncios autorizadas na Meta para um workspace")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<MetaAdAccount>>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get([FromQuery] Guid workspaceId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetMetaAdAccountsQuery(workspaceId), cancellationToken));

    /// <summary>Vincula contas cuja autorização é confirmada novamente na Meta.</summary>
    /// <param name="request">Workspace e IDs externos selecionados.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>IDs internos dos vínculos.</returns>
    [HttpPost("select")]
    [EndpointSummary("Seleciona contas Meta verificadas e vincula ao workspace")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<Guid>>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Select([FromBody] SelectMetaAdAccountsCommand request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(request, cancellationToken));

    /// <summary>Desvincula uma conta local sem revogar permissões de outros workspaces.</summary>
    /// <param name="accountId">ID interno do vínculo.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Resultado da desvinculação.</returns>
    [HttpDelete("{accountId:guid}")]
    [EndpointSummary("Desvincula uma conta Meta do workspace preservando o histórico")]
    [ProducesResponseType(typeof(Result), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Disconnect(Guid accountId, [FromQuery] Guid workspaceId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DisconnectMetaAccountsCommand(workspaceId, accountId), cancellationToken);
        return result.IsSuccess ? Ok(result) : StatusCode(Status(result.Error), result);
    }

    private IActionResult Respond<T>(Result<T> result) => result.IsSuccess ? Ok(result) : StatusCode(Status(result.Error), result);
    private static int Status(Error error) => error.Type switch
    { ErrorType.Unauthorized => 401, ErrorType.Forbidden => 403, ErrorType.NotFound => 404, ErrorType.Validation => 422, _ => 502 };
}
