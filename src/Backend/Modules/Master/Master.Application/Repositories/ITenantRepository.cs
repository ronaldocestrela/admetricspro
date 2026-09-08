using BuildingBlocks.Application.Persistence;
using Master.Domain.Tenants;

namespace Master.Application.Repositories;

/// <summary>
/// Repository contract for tenant aggregate persistence.
/// </summary>
public interface ITenantRepository : IRepository<Tenant, TenantId>
{
    /// <summary>
    /// Finds a tenant by its normalized subdomain.
    /// </summary>
    /// <param name="subdomain">Tenant subdomain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant aggregate if found; otherwise null.</returns>
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a tenant by its fiscal document (CPF or CNPJ).
    /// </summary>
    /// <param name="cnpj">Tax/fiscal document (digits-only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant aggregate if found; otherwise null.</returns>
    Task<Tenant?> GetByCnpjAsync(string cnpj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tenants that require dunning evaluation (tenants with an overdue payment date or non-None dunning stage).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of tenant aggregates requiring dunning evaluation.</returns>
    Task<IReadOnlyList<Tenant>> GetTenantsForDunningEvaluationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all tenants that require trial expiration notice evaluation (tenants currently in Trial status with an expiration date and admin email configured).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of tenant aggregates eligible for trial notice evaluation.</returns>
    Task<IReadOnlyList<Tenant>> GetTenantsForTrialNoticeEvaluationAsync(CancellationToken cancellationToken = default);
}