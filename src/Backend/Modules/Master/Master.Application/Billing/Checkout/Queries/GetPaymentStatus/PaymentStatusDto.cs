using Master.Domain.Billing;

namespace Master.Application.Billing.Checkout.Queries.GetPaymentStatus;

/// <summary>
/// Projeção com o estado atual de liquidação de uma transação financeira.
/// </summary>
/// <param name="TransactionId">Identificador único da transação.</param>
/// <param name="Status">Estado atual de liquidação.</param>
/// <param name="IsPaid">Indica se a cobrança foi confirmada e liquidada.</param>
/// <param name="PaidAtUtc">Data e hora UTC da liquidação confirmada.</param>
public sealed record PaymentStatusDto(
    Guid TransactionId,
    PaymentTransactionStatus Status,
    bool IsPaid,
    DateTime? PaidAtUtc);
