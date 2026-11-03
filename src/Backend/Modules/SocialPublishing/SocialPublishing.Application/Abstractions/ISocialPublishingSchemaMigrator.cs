using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Aplica migrações EF do schema <c>social</c> no banco dedicado do tenant.
/// </summary>
public interface ISocialPublishingSchemaMigrator
{
    /// <summary>
    /// Aplica migrações pendentes usando a connection string informada.
    /// </summary>
    /// <param name="connectionString">Connection string do banco do tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> ApplyAsync(string connectionString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve a connection string do tenant e aplica migrações pendentes.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> ApplyForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
