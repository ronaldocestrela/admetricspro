using BuildingBlocks.Application.Messaging;

namespace Master.Application.Billing.Checkout.Queries.GetPaymentStatus;

/// <summary>
/// Consulta para verificar o estado atual de liquidação de uma transação de pagamento.
/// </summary>
/// <param name="TransactionId">Identificador único da transação.</param>
public sealed record GetPaymentStatusQuery(Guid TransactionId) : IQuery<PaymentStatusDto>;
