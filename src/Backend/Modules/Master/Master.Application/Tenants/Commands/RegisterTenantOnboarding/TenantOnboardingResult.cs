using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.RegisterTenantOnboarding;

/// <summary>
/// Resultado estruturado retornado após a conclusão bem-sucedida do onboarding e provisionamento de novo inquilino.
/// </summary>
/// <param name="TenantId">Identificador único global do tenant criado.</param>
/// <param name="CompanyName">Razão social cadastrada.</param>
/// <param name="Subdomain">Subdomínio ativo atribuído à instância.</param>
/// <param name="AccessUrl">URL de redirecionamento para o ambiente e cockpit do tenant.</param>
/// <param name="AdminEmail">E-mail corporativo do gestor principal.</param>
/// <param name="Tier">Plano de assinatura provisionado.</param>
public sealed record TenantOnboardingResult(
    Guid TenantId,
    string CompanyName,
    string Subdomain,
    string AccessUrl,
    string AdminEmail,
    SubscriptionTier Tier);
