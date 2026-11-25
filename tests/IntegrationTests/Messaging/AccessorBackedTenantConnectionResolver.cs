using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;

namespace IntegrationTests.Messaging;

/// <summary>
/// Resolvedor de teste que usa <see cref="ITenantContextAccessor"/> para o tenant corrente.
/// </summary>
internal sealed class AccessorBackedTenantConnectionResolver(
    ITenantContextAccessor accessor,
    IReadOnlyDictionary<Guid, string> connections) : ITenantConnectionResolver
{
    /// <inheritdoc />
    public Task<Result<string>> ResolveConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(connections.TryGetValue(tenantId, out var connection)
            ? Result<string>.Success(connection)
            : Result<string>.Failure(Error.NotFound("Tenant.NotFound", "Connection string não configurada para o tenant.")));
    }

    /// <inheritdoc />
    public Task<Result<string>> ResolveConnectionStringBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<string>.Failure(Error.NotFound("Tenant.NotFound", "Subdomínio não configurado.")));

    /// <inheritdoc />
    public Task<Result<string>> ResolveCurrentTenantConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tenantId = accessor.TenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            return Task.FromResult(Result<string>.Failure(Error.Validation("Tenant.ContextMissing", "Tenant contextual ausente.")));
        }

        return ResolveConnectionStringAsync(tenantId.Value, cancellationToken);
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