using System.Text.Json;
using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Meta;
using Integrations.Domain.Meta;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

/// <summary>Gerenciamento explícito de campanhas, conjuntos, anúncios e criativos da Meta.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="operations">Diário idempotente das operações.</param>
[Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Tenant")]
[ApiController]
[Route("api/v1/integrations/meta")]
public sealed class MetaResourcesController(ISender sender, IMetaOperationRepository operations) : ControllerBase
{
    /// <summary>Obtém o catálogo versionado de configurações para a conta selecionada.</summary>
    /// <param name="workspaceId">Workspace autorizado.</param>
    /// <param name="accountId">Conta interna vinculada.</param>
    /// <param name="resource">Recurso oficial.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Tipos, opções, disponibilidade por operação e restrições documentadas.</returns>
    [HttpGet("resource-options")]
    [EndpointSummary("Lista configurações oficiais de campanhas, conjuntos, anúncios e criativos Meta")]
    [ProducesResponseType(typeof(Result<MetaResourceDefinition>), 200)]
    [ProducesResponseType(400)] [ProducesResponseType(401)] [ProducesResponseType(403)] [ProducesResponseType(404)] [ProducesResponseType(422)]
    public async Task<IActionResult> Options([FromQuery] Guid workspaceId, [FromQuery] Guid accountId, [FromQuery] string resource, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetMetaResourceOptionsQuery(workspaceId, accountId, resource), cancellationToken));

    /// <summary>Lê os valores atuais das configurações sem preencher padrões em campos indisponíveis.</summary>
    /// <param name="resource">Recurso oficial.</param>
    /// <param name="externalId">Objeto remoto.</param>
    /// <param name="workspaceId">Workspace autorizado.</param>
    /// <param name="accountId">Conta interna vinculada.</param>
    /// <param name="fields">Lista opcional separada por vírgulas para recarregar campos específicos.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Valores reais e falhas de leitura individuais.</returns>
    [HttpGet("resources/{resource}/{externalId}/configuration")]
    [EndpointSummary("Carrega configurações editáveis e somente de criação de um objeto Meta autorizado")]
    [ProducesResponseType(typeof(Result<MetaResourceConfiguration>), 200)]
    [ProducesResponseType(400)] [ProducesResponseType(401)] [ProducesResponseType(403)] [ProducesResponseType(404)] [ProducesResponseType(422)]
    public async Task<IActionResult> Configuration(string resource, string externalId, [FromQuery] Guid workspaceId, [FromQuery] Guid accountId,
        [FromQuery] string? fields, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetMetaResourceConfigurationQuery(workspaceId, accountId, resource, externalId,
            fields is null ? null : fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)), cancellationToken));

    /// <summary>Pesquisa opções oficiais de targeting para a conta autorizada.</summary>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="type">adinterest, adlocale ou adgeolocation.</param>
    /// <param name="query">Termo de pesquisa.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Opções elegíveis fornecidas pela Meta.</returns>
    [HttpGet("targeting-search")]
    [EndpointSummary("Pesquisa interesses, idiomas ou localizações na Meta")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<JsonElement>>), 200)]
    [ProducesResponseType(400)] [ProducesResponseType(401)] [ProducesResponseType(403)] [ProducesResponseType(404)] [ProducesResponseType(422)]
    public async Task<IActionResult> Search([FromQuery] Guid workspaceId, [FromQuery] Guid accountId, [FromQuery] string type, [FromQuery] string query, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SearchMetaTargetingQuery(workspaceId, accountId, type, query), cancellationToken));
    /// <summary>Consulta detalhes remotos após confirmar a conta proprietária.</summary>
    /// <param name="resource">Recurso permitido.</param>
    /// <param name="externalId">ID externo do objeto.</param>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna vinculada.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Campos oficiais selecionados.</returns>
    [HttpGet("resources/{resource}/{externalId}")]
    [EndpointSummary("Consulta detalhes e estado efetivo de um recurso Meta autorizado")]
    [ProducesResponseType(typeof(Result<JsonElement>), 200)]
    [ProducesResponseType(400)] [ProducesResponseType(401)] [ProducesResponseType(403)] [ProducesResponseType(404)] [ProducesResponseType(422)]
    public async Task<IActionResult> Get(string resource, string externalId, [FromQuery] Guid workspaceId, [FromQuery] Guid accountId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetMetaResourceQuery(workspaceId, accountId, resource, externalId), cancellationToken));

    /// <summary>Cria recurso pausado ou edita campos oficialmente suportados.</summary>
    /// <param name="request">Pedido com chave de idempotência, conta e campos.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>ID confirmado ou erro contextual.</returns>
    [HttpPost("resources")]
    [EndpointSummary("Cria ou edita um recurso Meta com idempotência e confirmação de ativação")]
    [ProducesResponseType(typeof(Result<MetaWriteResult>), 200)]
    [ProducesResponseType(400)] [ProducesResponseType(401)] [ProducesResponseType(403)] [ProducesResponseType(404)] [ProducesResponseType(409)] [ProducesResponseType(422)]
    public async Task<IActionResult> Write([FromBody] WriteMetaResourceCommand request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(request, cancellationToken));

    /// <summary>Lista ativos autorizados, sem enviar dados pessoais de públicos.</summary>
    /// <param name="asset">promote_pages, instagram_accounts, adspixels ou customaudiences.</param>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="accountId">Conta interna.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Ativos elegíveis.</returns>
    [HttpGet("assets/{asset}")]
    [EndpointSummary("Lista páginas, identidades Instagram, pixels ou públicos da conta Meta")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<JsonElement>>), 200)]
    [ProducesResponseType(400)] [ProducesResponseType(401)] [ProducesResponseType(403)] [ProducesResponseType(404)] [ProducesResponseType(422)]
    public async Task<IActionResult> Assets(string asset, [FromQuery] Guid workspaceId, [FromQuery] Guid accountId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetMetaAssetsQuery(workspaceId, accountId, asset), cancellationToken));

    /// <summary>Consulta o diário da operação sem reexecutar a escrita.</summary>
    /// <param name="operationId">Chave idempotente.</param>
    /// <param name="workspaceId">Workspace autorizado.</param>
    /// <param name="accountId">Conta interna autorizada.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Estado, recurso e datas, sem payload ou tokens.</returns>
    [HttpGet("operations/{operationId:guid}")]
    [EndpointSummary("Consulta o estado de uma operação Meta sem repetir POST")]
    [ProducesResponseType(200)] [ProducesResponseType(401)] [ProducesResponseType(403)] [ProducesResponseType(404)]
    public async Task<IActionResult> Operation(Guid operationId, [FromQuery] Guid workspaceId, [FromQuery] Guid accountId, CancellationToken cancellationToken)
    {
        var access = await sender.Send(new ValidateIntegrationWorkspaceQuery(workspaceId, false), cancellationToken);
        if (access.IsFailure || !access.Value) return StatusCode(403, Result.Failure(Error.Forbidden("Meta.Forbidden", "Sem permissão para esta operação.")));
        var operation = await operations.GetAsync(operationId, workspaceId, accountId, cancellationToken);
        return operation is null ? NotFound(Result.Failure(Error.NotFound("Meta.OperationNotFound", "Operação não encontrada."))) :
            Ok(Result<object>.Success(new { operation.Id, operation.State, operation.ExternalId, operation.ErrorCode, operation.CreatedAtUtc, operation.CompletedAtUtc }));
    }

    private IActionResult Respond<T>(Result<T> result) => result.IsSuccess ? Ok(result) : StatusCode(result.Error.Type switch
    { ErrorType.Unauthorized => 401, ErrorType.Forbidden => 403, ErrorType.NotFound => 404, ErrorType.Conflict => 409, ErrorType.Validation => 422, _ => 502 }, result);
}
