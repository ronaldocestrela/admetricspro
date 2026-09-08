using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.Tenants.Events;

/// <summary>
/// Evento de domínio emitido imediatamente após a conclusão bem-sucedida do provisionamento de um inquilino e seu banco de dados dedicado.
/// </summary>
/// <param name="TenantId">Identificador do tenant provisionado.</param>
/// <param name="CompanyName">Razão social ou nome fantasia da empresa.</param>
/// <param name="Subdomain">Subdomínio de roteamento exclusivo.</param>
/// <param name="AdminEmail">E-mail corporativo do administrador inicial (Owner).</param>
/// <param name="AdminFullName">Nome completo do administrador inicial.</param>
/// <param name="CustomDomain">Domínio CNAME customizado opcional.</param>
/// <param name="Tier">Plano de assinatura inicial.</param>
/// <param name="ProvisionedAtUtc">Data e hora da conclusão do provisionamento em UTC.</param>
public sealed record TenantProvisionedEvent(
    TenantId TenantId,
    string CompanyName,
    string Subdomain,
    string AdminEmail,
    string AdminFullName,
    string? CustomDomain,
    SubscriptionTier Tier,
    DateTime ProvisionedAtUtc) : IDomainEvent;
