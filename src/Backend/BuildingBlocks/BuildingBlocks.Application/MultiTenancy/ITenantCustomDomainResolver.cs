namespace BuildingBlocks.Application.MultiTenancy;

/// <summary>
/// Contrato de serviço para resolução e mapeamento de domínios personalizados (CNAME) para inquilinos.
/// </summary>
public interface ITenantCustomDomainResolver
{
    /// <summary>
    /// Resolve o inquilino proprietário de um domínio personalizado (CNAME) a partir do catálogo central.
    /// </summary>
    /// <param name="customDomain">Nome de domínio totalmente qualificado (FQDN) recebido no cabeçalho Host.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O mapeamento com ID e subdomínio do inquilino se encontrado; caso contrário, null.</returns>
    Task<TenantCustomDomainMapping?> ResolveTenantByCustomDomainAsync(
        string customDomain,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Registro imutável de mapeamento de domínio CNAME para o identificador e subdomínio do inquilino.
/// </summary>
/// <param name="TenantId">Identificador único global do inquilino no catálogo.</param>
/// <param name="Subdomain">Subdomínio/slug institucional do inquilino.</param>
/// <param name="CustomDomain">Domínio personalizado CNAME normalizado.</param>
public sealed record TenantCustomDomainMapping(
    Guid TenantId,
    string Subdomain,
    string CustomDomain);
