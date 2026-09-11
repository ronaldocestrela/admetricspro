using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Audit.DTOs;
using Tenants.Application.Rbac.DTOs;

namespace WebApp.Services;

/// <summary>
/// Contrato do cliente HTTP para gestão de controle de acesso (RBAC) e consulta da trilha de auditoria no frontend.
/// </summary>
public interface ITenantRbacClientService
{
    /// <summary>
    /// Consulta a matriz canônica completa de papéis e permissões do inquilino.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo a matriz de papéis e permissões.</returns>
    Task<Result<TenantRbacMatrixDto>> GetRbacMatrixAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o conjunto de permissões ativas de um colaborador específico.
    /// </summary>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo as permissões ativas do colaborador.</returns>
    Task<Result<TenantUserPermissionsDto>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Altera o papel funcional de um colaborador, disparando auditoria imutável no backend.
    /// </summary>
    /// <param name="userId">Identificador do colaborador alvo.</param>
    /// <param name="newRole">Novo papel funcional a ser atribuído.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> ChangeUserRoleAsync(Guid userId, TenantRole newRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta a trilha de auditoria do inquilino com paginação e filtros.
    /// </summary>
    /// <param name="userId">Filtro opcional por identificador do operador.</param>
    /// <param name="action">Filtro opcional por ação executada.</param>
    /// <param name="fromUtc">Filtro opcional por data inicial UTC.</param>
    /// <param name="toUtc">Filtro opcional por data final UTC.</param>
    /// <param name="page">Número da página.</param>
    /// <param name="pageSize">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo a lista paginada de registros de auditoria.</returns>
    Task<Result<TenantAuditLogsResponse>> GetAuditLogsAsync(
        Guid? userId = null,
        string? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
