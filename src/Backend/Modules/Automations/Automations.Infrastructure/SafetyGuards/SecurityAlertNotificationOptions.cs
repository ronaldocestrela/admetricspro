namespace Automations.Infrastructure.SafetyGuards;

/// <summary>
/// Opções de configuração para despacho de alertas e alarmes de segurança operacional.
/// </summary>
public sealed class SecurityAlertNotificationOptions
{
    /// <summary>
    /// Nome da seção no arquivo appsettings.json.
    /// </summary>
    public const string SectionName = "SecurityAlerts";

    /// <summary>
    /// URL padrão do Incoming Webhook do canal corporativo do Slack.
    /// </summary>
    public string? SlackWebhookUrl { get; set; }

    /// <summary>
    /// Telefone padrão de contato comercial para alertas urgentes via WhatsApp (+5511...).
    /// </summary>
    public string? WhatsAppRecipientPhone { get; set; }

    /// <summary>
    /// Endereço de e-mail central para envio de alertas críticos da operação.
    /// </summary>
    public string? AlertRecipientEmail { get; set; }

    /// <summary>
    /// URL de endpoint HTTP POST para integração com webhook genérico de terceiros.
    /// </summary>
    public string? GenericWebhookUrl { get; set; }

    /// <summary>
    /// Chave secreta de autenticação / assinatura HMAC enviada no cabeçalho 'X-Security-Alert-Token'.
    /// </summary>
    public string? GenericWebhookSecret { get; set; }
}
