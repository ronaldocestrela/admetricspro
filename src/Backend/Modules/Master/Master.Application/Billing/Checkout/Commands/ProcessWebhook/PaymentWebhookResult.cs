using Master.Domain.Billing;

namespace Master.Application.Billing.Checkout.Commands.ProcessWebhook;

/// <summary>
/// Resultado do processamento de uma notificação assíncrona (webhook) de pagamento.
/// </summary>
/// <param name="IsProcessed">Indica se a notificação foi processada com sucesso.</param>
/// <param name="IsAlreadyProcessed">Indica se a notificação já havia sido processada previamente (idempotência).</param>
/// <param name="TransactionId">Identificador único da transação afetada se localizada.</param>
/// <param name="Status">Estado final de liquidação da cobrança.</param>
public sealed record PaymentWebhookResult(
    bool IsProcessed,
    bool IsAlreadyProcessed,
    Guid? TransactionId,
    PaymentTransactionStatus Status);
