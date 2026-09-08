namespace Master.Domain.Billing;

/// <summary>
/// Especifica a modalidade de pagamento utilizada na transação financeira.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Pagamento via Cartão de Crédito.
    /// </summary>
    CreditCard = 1,

    /// <summary>
    /// Pagamento instantâneo via Pix (Banco Central do Brasil).
    /// </summary>
    Pix = 2
}
