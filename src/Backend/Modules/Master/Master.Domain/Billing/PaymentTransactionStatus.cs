namespace Master.Domain.Billing;

/// <summary>
/// Especifica o estado de processamento de uma transação financeira de cobrança.
/// </summary>
public enum PaymentTransactionStatus
{
    /// <summary>
    /// Cobrança emitida e aguardando processamento/pagamento.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Cobrança liquidada e aprovada com sucesso.
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Cobrança recusada, expirada ou com falha de processamento.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Cobrança cancelada antes da liquidação.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Cobrança estornada após confirmação prévia.
    /// </summary>
    Refunded = 5
}
