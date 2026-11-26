namespace BuildingBlocks.Application.MultiTenancy;

/// <summary>
/// Contexto de tenant imutável para escopos definidos em código (ex.: token HMAC de mídia pública).
/// </summary>
public sealed class ScopedTenantContext : ITenantContext
{
    /// <summary>
    /// Inicializa o contexto com tenant e origem informados.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="source">Canal de resolução.</param>
    /// <param name="rawIdentifier">Identificador bruto opcional.</param>
    /// <param name="subdomain">Subdomínio opcional.</param>
    public ScopedTenantContext(
        Guid tenantId,
        TenantResolutionSource source,
        string? rawIdentifier = null,
        string? subdomain = null)
    {
        TenantId = tenantId;
        Source = source;
        RawIdentifier = rawIdentifier;
        Subdomain = subdomain;
    }

    /// <inheritdoc />
    public Guid? TenantId { get; }

    /// <inheritdoc />
    public string? Subdomain { get; }

    /// <inheritdoc />
    public string? RawIdentifier { get; }

    /// <inheritdoc />
    public TenantResolutionSource Source { get; }

    /// <inheritdoc />
    public bool IsResolved => TenantId.HasValue && TenantId != Guid.Empty;
}