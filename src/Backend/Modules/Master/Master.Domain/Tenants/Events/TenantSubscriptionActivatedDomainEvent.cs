using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.Tenants.Events;

/// <summary>
/// Evento de domínio disparado após a liquidação do pagamento e transição definitiva de um tenant para assinatura comercial ativa.
/// </summary>
/// <param name="TenantId">Identificador único do tenant ativado.</param>
/// <param name="CompanyName">Razão social ou nome fantasia da empresa.</param>
/// <param name="AdminEmail">E-mail corporativo do administrador principal.</param>
/// <param name="Tier">Nível do plano de assinatura contratado.</param>
/// <param name="BillingCycle">Ciclo de faturamento contratado (Monthly ou Annual).</param>
/// <param name="Amount">Valor monetário total pago na transação.</param>
/// <param name="PaidAtUtc">Data e hora UTC da liquidação financeira.</param>
/// <param name="ExpiresAtUtc">Data e hora UTC prevista para a próxima renovação da assinatura.</param>
public sealed record TenantSubscriptionActivatedDomainEvent(
    TenantId TenantId,
    string CompanyName,
    string? AdminEmail,
    SubscriptionTier Tier,
    string BillingCycle,
    decimal Amount,
    DateTime PaidAtUtc,
    DateTime ExpiresAtUtc) : IDomainEvent;
