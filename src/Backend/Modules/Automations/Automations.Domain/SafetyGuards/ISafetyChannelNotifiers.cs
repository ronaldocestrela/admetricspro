using BuildingBlocks.Domain.Automations.SafetyGuards;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Domain.SafetyGuards;

/// <summary>
/// Contrato do despachador de notificações para canais corporativos do Slack via Incoming Webhooks.
/// </summary>
public interface ISlackWebhookNotifier
{
    /// <summary>
    /// Despacha uma mensagem formatada com Block Kit para o webhook do Slack.
    /// </summary>
    /// <param name="webhookUrl">URL do Incoming Webhook do canal do Slack.</param>
    /// <param name="payload">Dados estruturados do alerta de segurança.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado indicando sucesso ou erro tipado.</returns>
    Task<Result> SendSlackAlertAsync(string webhookUrl, SafetyAlertPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contrato do despachador de notificações para WhatsApp comercial via gateway/webhook HTTP.
/// </summary>
public interface IWhatsAppWebhookNotifier
{
    /// <summary>
    /// Despacha uma mensagem estruturada e urgente para o WhatsApp comercial configurado.
    /// </summary>
    /// <param name="recipientPhone">Número de telefone de destino no formato internacional E.164.</param>
    /// <param name="payload">Dados estruturados do alerta de segurança.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado indicando sucesso ou erro tipado.</returns>
    Task<Result> SendWhatsAppAlertAsync(string recipientPhone, SafetyAlertPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contrato do despachador de e-mails de alerta crítico e emergencial.
/// </summary>
public interface IEmailAlertNotifier
{
    /// <summary>
    /// Despacha um e-mail transacional estilizado com template de alta visibilidade e dados do incidente.
    /// </summary>
    /// <param name="recipientEmail">Endereço de e-mail do destinatário.</param>
    /// <param name="payload">Dados estruturados do alerta de segurança.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado indicando sucesso ou erro tipado.</returns>
    Task<Result> SendEmailAlertAsync(string recipientEmail, SafetyAlertPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contrato do despachador de alertas para Webhooks genéricos HTTP POST integrados a sistemas externos.
/// </summary>
public interface IGenericWebhookNotifier
{
    /// <summary>
    /// Despacha um payload JSON para um endpoint de webhook configurado.
    /// </summary>
    /// <param name="endpointUrl">URL de destino do Webhook.</param>
    /// <param name="payload">Dados estruturados do alerta de segurança.</param>
    /// <param name="secretToken">Token opcional de assinatura / autenticação no cabeçalho HTTP.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado indicando sucesso ou erro tipado.</returns>
    Task<Result> SendWebhookAlertAsync(string endpointUrl, SafetyAlertPayload payload, string? secretToken = null, CancellationToken cancellationToken = default);
}
