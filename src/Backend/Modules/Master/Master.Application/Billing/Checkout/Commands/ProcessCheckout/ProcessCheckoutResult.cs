using Master.Domain.Billing;
using Master.Domain.Tenants;

namespace Master.Application.Billing.Checkout.Commands.ProcessCheckout;

/// <summary>
/// Resultado da execução de uma operação de checkout financeiro.
/// </summary>
/// <param name="TransactionId">Identificador único global da transação criada.</param>
/// <param name="TenantId">Identificador do tenant contratante.</param>
/// <param name="Tier">Nível do plano contratado.</param>
/// <param name="BillingCycle">Ciclo de faturamento (Monthly ou Annual).</param>
/// <param name="PaymentMethod">Método de pagamento empregado.</param>
/// <param name="Amount">Valor monetário total cobrado.</param>
/// <param name="Status">Estado de processamento da transação.</param>
/// <param name="IsActivated">Indica se a transição para plano ativo foi concluída imediatamente.</param>
/// <param name="PixQrCode">QR Code do Pix em Base64 quando método Pix for utilizado.</param>
/// <param name="PixCopiaECola">Chave Copia e Cola EMV do Pix quando método Pix for utilizado.</param>
/// <param name="PixExpiresAtUtc">Data e hora limite para pagamento do Pix.</param>
/// <param name="FailureReason">Mensagem explicativa caso o pagamento tenha sido recusado.</param>
public sealed record ProcessCheckoutResult(
    Guid TransactionId,
    Guid TenantId,
    SubscriptionTier Tier,
    string BillingCycle,
    PaymentMethod PaymentMethod,
    decimal Amount,
    PaymentTransactionStatus Status,
    bool IsActivated,
    string? PixQrCode = null,
    string? PixCopiaECola = null,
    DateTime? PixExpiresAtUtc = null,
    string? FailureReason = null);
