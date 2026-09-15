namespace Master.Application.Tenants.Queries.GetTenantCustomDomain;

/// <summary>
/// DTO com os detalhes de configuração do domínio personalizado CNAME do inquilino.
/// </summary>
/// <param name="TenantId">Identificador do inquilino.</param>
/// <param name="CustomDomain">Domínio personalizado CNAME configurado atualmente, se houver.</param>
/// <param name="ExpectedCnameTarget">Destino canônico CNAME esperado para apontamento DNS.</param>
/// <param name="IsConfigured">Indica se o inquilino já possui um domínio customizado salvo.</param>
/// <param name="HasPlanSupport">Indica se o plano atual do inquilino inclui o recurso de CNAME customizado.</param>
public sealed record TenantCustomDomainDto(
    Guid TenantId,
    string? CustomDomain,
    string ExpectedCnameTarget,
    bool IsConfigured,
    bool HasPlanSupport);
