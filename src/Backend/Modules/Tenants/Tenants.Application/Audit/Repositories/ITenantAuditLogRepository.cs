using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Audit.Repositories;

/// <summary>
/// Contrato de repositório para persistência e consulta da trilha de auditoria imutável do inquilino.
/// </summary>
public interface ITenantAuditLogRepository
{
    /// <summary>
    /// Adiciona uma nova entrada imutável de auditoria no banco de dados do inquilino.
    /// </summary>
    /// <param name="entry">Entidade de auditoria instanciada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(TenantAuditLog entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta os registros de auditoria com suporte a filtros e paginação.
    /// </summary>
    /// <param name="userId">Filtro opcional por identificador do usuário.</param>
    /// <param name="action">Filtro opcional por ação executada.</param>
    /// <param name="fromUtc">Filtro opcional por data inicial UTC.</param>
    /// <param name="toUtc">Filtro opcional por data final UTC.</param>
    /// <param name="page">Número da página (1-based).</param>
    /// <param name="pageSize">Quantidade de itens por página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista paginada de registros de auditoria ordenados decrescente por data.</returns>
    Task<IReadOnlyList<TenantAuditLog>> GetLogsAsync(
        Guid? userId = null,
        string? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a contagem total de registros de auditoria para os filtros especificados.
    /// </summary>
    /// <param name="userId">Filtro opcional por identificador do usuário.</param>
    /// <param name="action">Filtro opcional por ação executada.</param>
    /// <param name="fromUtc">Filtro opcional por data inicial UTC.</param>
    /// <param name="toUtc">Filtro opcional por data final UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Total de registros correspondentes aos filtros.</returns>
    Task<int> GetTotalCountAsync(
        Guid? userId = null,
        string? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);
}
