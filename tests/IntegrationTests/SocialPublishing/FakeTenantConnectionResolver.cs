using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;

namespace IntegrationTests.SocialPublishing;

/// <summary>
/// Resolvedor de connection string em memória para testes de integração multi-tenant.
/// </summary>
internal sealed class FakeTenantConnectionResolver : ITenantConnectionResolver
{
    private readonly IReadOnlyDictionary<Guid, string> _connectionStringsByTenant;
    private Guid _currentTenantId;

    /// <summary>
    /// Inicializa o resolvedor com mapa tenant → connection string.
    /// </summary>
    /// <param name="connectionStringsByTenant">Mapa de tenants de teste.</param>
    public FakeTenantConnectionResolver(IReadOnlyDictionary<Guid, string> connectionStringsByTenant)
    {
        _connectionStringsByTenant = connectionStringsByTenant;
    }

    /// <summary>
    /// Define o tenant contextual simulado para <see cref="ResolveCurrentTenantConnectionStringAsync"/>.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant corrente.</param>
    public void SetCurrentTenant(Guid tenantId) => _currentTenantId = tenantId;

    /// <inheritdoc />
    public Task<Result<string>> ResolveConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_connectionStringsByTenant.TryGetValue(tenantId, out var connectionString)
            ? Result<string>.Success(connectionString)
            : Result<string>.Failure(Error.NotFound("Tenant.NotFound", "Tenant de teste não encontrado.")));
    }

    /// <inheritdoc />
    public Task<Result<string>> ResolveConnectionStringBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<string>.Failure(Error.NotFound("Tenant.NotFound", "Subdomínio não configurado no teste.")));

    /// <inheritdoc />
    public Task<Result<string>> ResolveCurrentTenantConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ResolveConnectionStringAsync(_currentTenantId, cancellationToken);
    }

    /// <inheritdoc />
    public void InvalidateCache(Guid tenantId)
    {
    }

    /// <inheritdoc />
    public void InvalidateCacheBySubdomain(string subdomain)
    {
    }
}
