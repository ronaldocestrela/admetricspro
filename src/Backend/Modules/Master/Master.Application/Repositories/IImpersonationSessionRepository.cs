using BuildingBlocks.Application.Persistence;
using Master.Domain.Tenants;

namespace Master.Application.Repositories;

/// <summary>
/// Repository contract for managing persistence and retrieval of impersonation sessions.
/// </summary>
public interface IImpersonationSessionRepository : IRepository<ImpersonationSession, Guid>
{
    /// <summary>
    /// Retrieves all active impersonation sessions for a given tenant identifier.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active impersonation sessions.</returns>
    Task<IReadOnlyList<ImpersonationSession>> GetActiveByTenantIdAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active session by its unique identifier and evaluates if it is unexpired and unrevoked.
    /// </summary>
    /// <param name="sessionId">Impersonation session identifier.</param>
    /// <param name="referenceUtc">UTC reference timestamp for evaluation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active session if found and valid; otherwise null.</returns>
    Task<ImpersonationSession?> GetActiveSessionByIdAsync(
        Guid sessionId,
        DateTime referenceUtc,
        CancellationToken cancellationToken = default);
}
