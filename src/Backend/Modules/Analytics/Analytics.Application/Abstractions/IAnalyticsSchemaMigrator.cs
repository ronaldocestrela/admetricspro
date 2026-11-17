using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Abstractions;

/// <summary>
/// Aplica migrações EF do schema analytics no banco dedicado do tenant.
/// </summary>
public interface IAnalyticsSchemaMigrator
{
    /// <summary>
    /// Aplica migrações pendentes usando uma connection string explícita.
    /// </summary>
    /// <param name="connectionString">Connection string do tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> ApplyAsync(string connectionString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aplica migrações pendentes para um tenant do catálogo Master.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> ApplyForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
