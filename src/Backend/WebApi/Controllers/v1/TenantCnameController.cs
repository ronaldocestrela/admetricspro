using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Tenants.Commands.ConfigureTenantCustomDomain;
using Master.Application.Tenants.Queries.GetTenantCustomDomain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pelo gerenciamento de domínios personalizados CNAME e instruções DNS para White-Label.
/// </summary>
[ApiController]
[Route("api/v1/tenants/cname")]
public sealed class TenantCnameController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantCnameController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory para envio de comandos e consultas.</param>
    /// <param name="tenantContextAccessor">Acessor do contexto do inquilino ativo.</param>
    public TenantCnameController(
        ISender sender,
        ITenantContextAccessor tenantContextAccessor)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <summary>
    /// Obtém as instruções de apontamento DNS e o status da configuração de domínio customizado CNAME do inquilino ativo.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados do CNAME e status de configuração.</returns>
    [HttpGet]
    [EndpointSummary("Obtém o status do domínio CNAME e instruções DNS do inquilino ativo")]
    [ProducesResponseType(typeof(Result<TenantCustomDomainDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TenantCustomDomainDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<TenantCustomDomainDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<TenantCustomDomainDto>>> GetCnameDetails(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContextAccessor.TenantContext.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            return Unauthorized(Result<TenantCustomDomainDto>.Failure(
                Error.Unauthorized("Tenant.Unresolved", "Contexto de inquilino não identificado para a requisição.")));
        }

        var result = await _sender.Send(new GetTenantCustomDomainQuery(tenantId.Value), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                ErrorType.Unauthorized => Unauthorized(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Configura ou atualiza o domínio CNAME personalizado do inquilino no catálogo MasterDb.
    /// </summary>
    /// <param name="request">Dados do domínio a ser configurado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPut]
    [EndpointSummary("Configura ou altera o domínio personalizado CNAME do inquilino")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result>> ConfigureCname(
        [FromBody] ConfigureTenantCustomDomainApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var tenantId = _tenantContextAccessor.TenantContext.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            return Unauthorized(Result.Failure(
                Error.Unauthorized("Tenant.Unresolved", "Contexto de inquilino não identificado para a requisição.")));
        }

        var command = new ConfigureTenantCustomDomainCommand(tenantId.Value, request.CustomDomain);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Conflict => Conflict(result),
                ErrorType.Validation => UnprocessableEntity(result),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }

    /// <summary>
    /// Remove o apontamento de domínio CNAME personalizado do inquilino no catálogo MasterDb.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da remoção.</returns>
    [HttpDelete]
    [EndpointSummary("Remove a vinculação do domínio CNAME personalizado do inquilino")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> RemoveCname(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContextAccessor.TenantContext.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            return Unauthorized(Result.Failure(
                Error.Unauthorized("Tenant.Unresolved", "Contexto de inquilino não identificado para a requisição.")));
        }

        var command = new RemoveTenantCustomDomainCommand(tenantId.Value);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}
