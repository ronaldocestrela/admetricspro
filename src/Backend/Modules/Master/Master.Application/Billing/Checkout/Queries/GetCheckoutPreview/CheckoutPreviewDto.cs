using Master.Domain.Tenants;

namespace Master.Application.Billing.Checkout.Queries.GetCheckoutPreview;

/// <summary>
/// Projeção com dados consolidados de valores, descontos e quotas para pré-visualização de checkout.
/// </summary>
/// <param name="Tier">Nível do plano pretendido.</param>
/// <param name="PlanName">Nome comercial de exibição do plano.</param>
/// <param name="BillingCycle">Ciclo de faturamento (Monthly ou Annual).</param>
/// <param name="BaseMonthlyPrice">Preço base mensal de tabela em BRL.</param>
/// <param name="DiscountPercentage">Percentual de desconto aplicado para ciclo anual (0 a 100).</param>
/// <param name="TotalPayableNow">Valor líquido total a ser liquidado imediatamente na contratação.</param>
/// <param name="SavingsAmount">Economia monetária total proporcionada pelo plano anual.</param>
/// <param name="NextRenewalDateUtc">Data e hora UTC estimada para o próximo ciclo de renovação.</param>
public sealed record CheckoutPreviewDto(
    SubscriptionTier Tier,
    string PlanName,
    string BillingCycle,
    decimal BaseMonthlyPrice,
    int DiscountPercentage,
    decimal TotalPayableNow,
    decimal SavingsAmount,
    DateTime NextRenewalDateUtc);
