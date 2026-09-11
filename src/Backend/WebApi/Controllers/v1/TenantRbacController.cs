using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenants.Application.Audit.DTOs;
using Tenants.Application.Audit.Queries.GetTenantAuditLogs;
using Tenants.Application.Rbac.DTOs;
using Tenants.Application.Rbac.Queries.GetTenantRbacMatrix;
using Tenants.Application.Rbac.Queries.GetTenantUserPermissions;
using Tenants.Application.Users.Commands.ChangeTenantUserRole;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável pela governança de controle de acesso (RBAC) e consulta da trilha de auditoria do inquilino.
/// </summary>
[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantRbacController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserContext _currentUserContext;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantRbacController"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory para envio de comandos e consultas.</param>
    /// <param name="currentUserContext">Provedor contextual do usuário autenticado.</param>
    public TenantRbacController(
        ISender sender,
        ICurrentUserContext currentUserContext)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    /// <summary>
    /// Obtém a matriz completa de papéis e permissões canônicas do sistema.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Matriz de papéis com suas permissões atribuídas.</returns>
    [HttpGet("rbac/matrix")]
    [EndpointSummary("Retorna a matriz canônica de papéis e permissões granulares do inquilino")]
    [ProducesResponseType(typeof(Result<TenantRbacMatrixDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<TenantRbacMatrixDto>>> GetRbacMatrix(
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetTenantRbacMatrixQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém o conjunto de permissões ativas de um colaborador específico.
    /// </summary>
    /// <param name="id">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de permissões ativas do colaborador.</returns>
    [HttpGet("rbac/users/{id:guid}/permissions")]
    [EndpointSummary("Retorna as permissões efetivas atribuídas ao colaborador especificado")]
    [ProducesResponseType(typeof(Result<TenantUserPermissionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TenantUserPermissionsDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<TenantUserPermissionsDto>>> GetUserPermissions(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetTenantUserPermissionsQuery(id), cancellationToken);

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

    /// <summary>
    /// Altera o papel funcional de um colaborador, gravando o evento de forma imutável na trilha de auditoria.
    /// </summary>
    /// <param name="id">Identificador do colaborador alvo.</param>
    /// <param name="request">Novo papel solicitado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da alteração de papel.</returns>
    [HttpPut("users/{id:guid}/role")]
    [EndpointSummary("Altera o papel funcional de um colaborador com gravação de auditoria")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result>> ChangeUserRole(
        [FromRoute] Guid id,
        [FromBody] ChangeTenantUserRoleApiRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest(Result.Failure(
                Error.Validation("Request.Null", "O corpo da requisição não pode ser nulo.")));
        }

        var operatorId = _currentUserContext.UserId ?? Guid.Empty;
        var operatorEmail = _currentUserContext.UserEmail ?? "system@internal";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var command = new ChangeTenantUserRoleCommand(
            id,
            request.NewRole,
            operatorId,
            operatorEmail,
            ipAddress);

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

    /// <summary>
    /// Consulta os registros da trilha de auditoria imutável do inquilino com suporte a paginação e filtros.
    /// </summary>
    /// <param name="userId">Filtro opcional por identificador de usuário.</param>
    /// <param name="action">Filtro opcional por ação executada.</param>
    /// <param name="fromUtc">Filtro opcional por data inicial UTC.</param>
    /// <param name="toUtc">Filtro opcional por data final UTC.</param>
    /// <param name="page">Número da página (padrão: 1).</param>
    /// <param name="pageSize">Tamanho da página (padrão: 50).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista paginada de registros de auditoria.</returns>
    [HttpGet("audit-logs")]
    [EndpointSummary("Lista a trilha de auditoria operacional do inquilino")]
    [ProducesResponseType(typeof(Result<TenantAuditLogsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<TenantAuditLogsResponse>>> GetAuditLogs(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTenantAuditLogsQuery(userId, action, fromUtc, toUtc, page, pageSize);
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
