namespace Master.Application.Billing.Payments;

/// <summary>
/// Opções de configuração para validação de segurança e autenticação de webhooks de pagamento.
/// </summary>
public sealed class PaymentWebhookOptions
{
    /// <summary>
    /// Chave de seção para mapeamento nas configurações da aplicação.
    /// </summary>
    public const string SectionName = "PaymentWebhook";

    /// <summary>
    /// Token de validação exigido no cabeçalho HTTP das requisições de webhook.
    /// </summary>
    public string WebhookToken { get; set; } = string.Empty;
}
