using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Factory contract for dynamically instantiating tenant-scoped DbContext instances with runtime connection strings.
/// </summary>
/// <typeparam name="TContext">The tenant DbContext type.</typeparam>
public interface ITenantDbContextFactory<TContext> where TContext : DbContext
{
    /// <summary>
    /// Creates a DbContext instance configured for the active contextual tenant in the current execution scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the configured DbContext instance or failure.</returns>
    Task<Result<TContext>> CreateDbContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a DbContext instance explicitly configured for the specified tenant identifier.
    /// </summary>
    /// <param name="tenantId">The unique tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the configured DbContext instance or failure.</returns>
    Task<Result<TContext>> CreateDbContextAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
