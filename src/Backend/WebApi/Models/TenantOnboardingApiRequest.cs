using Master.Domain.Tenants;

namespace WebApi.Models;

/// <summary>
/// Modelo de requisição HTTP para submissão do fluxo de onboarding e provisionamento de novo inquilino.
/// </summary>
/// <param name="CompanyName">Razão social ou nome comercial da empresa.</param>
/// <param name="Cnpj">CNPJ numérico de 14 dígitos.</param>
/// <param name="Subdomain">Subdomínio desejado para o ambiente dedicado.</param>
/// <param name="Segment">Segmento de mercado de atuação.</param>
/// <param name="MonthlyAdSpendRange">Faixa estimada de investimento mensal em mídia.</param>
/// <param name="Tier">Plano de assinatura inicial escolhido.</param>
/// <param name="BillingCycle">Ciclo de cobrança (Monthly ou Annual).</param>
/// <param name="AdminFullName">Nome completo do administrador responsável.</param>
/// <param name="AdminEmail">E-mail corporativo do administrador.</param>
/// <param name="AdminPhone">Telefone de contato comercial do administrador.</param>
/// <param name="AdminPassword">Senha de acesso do administrador.</param>
/// <param name="CustomDomain">Domínio CNAME próprio opcional.</param>
/// <param name="PrimaryColor">Código hexadecimal da cor primária personalizada.</param>
/// <param name="SecondaryColor">Código hexadecimal da cor secundária personalizada.</param>
public sealed record TenantOnboardingApiRequest(
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
    string? SecondaryColor = null);
