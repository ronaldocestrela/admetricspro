using BuildingBlocks.Application.Messaging;
using Master.Domain.Billing;
using Master.Domain.Tenants;

namespace Master.Application.Billing.Checkout.Commands.ProcessCheckout;

/// <summary>
/// Comando para processar o checkout de contratação e transição de trial para plano pago.
/// </summary>
/// <param name="TenantId">Identificador do tenant contratante.</param>
/// <param name="Tier">Plano de assinatura pretendido.</param>
/// <param name="BillingCycle">Ciclo de faturamento (Monthly ou Annual).</param>
/// <param name="PaymentMethod">Método de pagamento (CreditCard ou Pix).</param>
/// <param name="CardHolderName">Nome do titular conforme impresso no cartão (quando Cartão).</param>
/// <param name="CardNumber">Número do cartão de crédito (quando Cartão).</param>
/// <param name="ExpiryMonth">Mês de vencimento com 2 dígitos (quando Cartão).</param>
/// <param name="ExpiryYear">Ano de vencimento com 4 dígitos (quando Cartão).</param>
/// <param name="Ccv">Código de segurança CCV/CVV (quando Cartão).</param>
public sealed record ProcessCheckoutCommand(
    Guid TenantId,
    SubscriptionTier Tier,
    string BillingCycle,
    PaymentMethod PaymentMethod,
    string? CardHolderName = null,
    string? CardNumber = null,
    string? ExpiryMonth = null,
    string? ExpiryYear = null,
    string? Ccv = null) : ICommand<ProcessCheckoutResult>;
