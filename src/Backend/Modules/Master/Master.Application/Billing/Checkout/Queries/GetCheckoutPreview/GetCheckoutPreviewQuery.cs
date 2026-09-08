using BuildingBlocks.Application.Messaging;
using Master.Domain.Tenants;

namespace Master.Application.Billing.Checkout.Queries.GetCheckoutPreview;

/// <summary>
/// Consulta para calcular e pré-visualizar a discriminação de valores e descontos antes do pagamento.
/// </summary>
/// <param name="TenantId">Identificador único do inquilino.</param>
/// <param name="Tier">Plano de assinatura pretendido.</param>
/// <param name="BillingCycle">Ciclo de faturamento pretendido (Monthly ou Annual).</param>
public sealed record GetCheckoutPreviewQuery(
    Guid TenantId,
    SubscriptionTier Tier,
    string BillingCycle) : IQuery<CheckoutPreviewDto>;
