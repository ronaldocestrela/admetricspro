using BuildingBlocks.Domain.Primitives;
using Master.Application.Tenants.Commands.ImpersonateTenant;
using Master.Domain.Tenants;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela gestão de tenants, workspaces e operações de suporte técnico seguro (Shadow Mode).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class TenantsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantsController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory de comandos e consultas.</param>
    public TenantsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Emite um token JWT de impersonação contextual seguro com claims de auditoria para suporte técnico.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant a ser acessado em Shadow Mode.</param>
    /// <param name="request">Parâmetros de justificativa e identificação de suporte.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resultado contendo o token de acesso e metadados da sessão.</returns>
    [HttpPost("{tenantId:guid}/impersonate")]
    [EndpointSummary("Emite token JWT contextual de impersonação (Shadow Mode) para suporte técnico auditado")]
    [ProducesResponseType(typeof(Result<ImpersonateTenantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ImpersonateTenantResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ImpersonateTenantResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<ImpersonateTenantResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<ImpersonateTenantResponse>>> ImpersonateTenant(
        [FromRoute] Guid tenantId,
        [FromBody] ImpersonateTenantApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result<ImpersonateTenantResponse>.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var command = new ImpersonateTenantCommand(
            new TenantId(tenantId),
            request.SuperAdminId,
            request.SupportTicketId,
            request.Reason,
            request.DurationMinutes);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<ImpersonateTenantResponse>.Failure(result.Error)),
                ErrorType.Validation => UnprocessableEntity(Result<ImpersonateTenantResponse>.Failure(result.Error)),
                _ => BadRequest(Result<ImpersonateTenantResponse>.Failure(result.Error))
            };
        }

        return Ok(Result<ImpersonateTenantResponse>.Success(result.Value));
    }

    /// <summary>
    /// Encerra imediatamente uma sessão de Shadow Mode ativa, revogando seu acesso e registrando na trilha de auditoria global.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant sob impersonação.</param>
    /// <param name="sessionId">Identificador único da sessão a ser revogada.</param>
    /// <param name="request">Carga opcional contendo justificativa de encerramento.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPost("{tenantId:guid}/impersonate/{sessionId:guid}/terminate")]
    [EndpointSummary("Encerra imediatamente uma sessão de impersonation (Shadow Mode) ativa")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result>> TerminateImpersonation(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid sessionId,
        [FromBody] TerminateImpersonationApiRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new Master.Application.Tenants.Commands.TerminateImpersonationSession.TerminateImpersonationSessionCommand(
            tenantId,
            sessionId,
            request?.Reason);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result),
                ErrorType.Validation => UnprocessableEntity(result),
                _ => BadRequest(result)
            };
        }

        return Ok(result);
    }
}
