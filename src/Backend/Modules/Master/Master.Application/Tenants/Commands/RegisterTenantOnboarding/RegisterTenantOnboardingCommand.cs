using BuildingBlocks.Application.Messaging;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.RegisterTenantOnboarding;

/// <summary>
/// Comando estruturado para registro e provisionamento de novo inquilino via fluxo de Onboarding.
/// </summary>
/// <param name="CompanyName">Razão social ou nome fantasia da empresa.</param>
/// <param name="Cnpj">CNPJ numérico de 14 dígitos da empresa.</param>
/// <param name="Subdomain">Subdomínio exclusivo alocado para a instância do tenant.</param>
/// <param name="Segment">Segmento de mercado ou área de atuação da organização.</param>
/// <param name="MonthlyAdSpendRange">Faixa estimada de investimento mensal em mídia paga.</param>
/// <param name="Tier">Tier de assinatura selecionado (Trial, Starter, Pro, Enterprise).</param>
/// <param name="BillingCycle">Ciclo de faturamento contratado (Monthly ou Annual).</param>
/// <param name="AdminFullName">Nome completo do gestor / super usuário da conta.</param>
/// <param name="AdminEmail">E-mail corporativo do gestor para acesso e notificações.</param>
/// <param name="AdminPhone">Telefone / WhatsApp comercial do gestor.</param>
/// <param name="AdminPassword">Senha de acesso do gestor.</param>
/// <param name="CustomDomain">Domínio CNAME personalizado opcional (recurso White-Label).</param>
/// <param name="PrimaryColor">Código hexadecimal da cor primária personalizada do tema.</param>
/// <param name="SecondaryColor">Código hexadecimal da cor secundária personalizada do tema.</param>
public sealed record RegisterTenantOnboardingCommand(
    string CompanyName,
    string Cnpj,
    string Subdomain,
    string Segment,
    string MonthlyAdSpendRange,
    SubscriptionTier Tier,
    string BillingCycle,
    string AdminFullName,
    string AdminEmail,
    string AdminPhone,
    string AdminPassword,
    string? CustomDomain = null,
    string? PrimaryColor = null,
    string? SecondaryColor = null) : ICommand<TenantOnboardingResult>;
