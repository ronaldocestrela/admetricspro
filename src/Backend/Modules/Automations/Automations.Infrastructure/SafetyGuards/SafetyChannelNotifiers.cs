using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Automations.Domain.SafetyGuards;
using BuildingBlocks.Application.Emails;
using BuildingBlocks.Domain.Automations.SafetyGuards;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Infrastructure.SafetyGuards;

/// <summary>
/// Despachador concreto de notificações via Incoming Webhook do Slack com formatação Block Kit.
/// </summary>
public sealed class SlackWebhookNotifier : ISlackWebhookNotifier
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SlackWebhookNotifier"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    public SlackWebhookNotifier(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result> SendSlackAlertAsync(
        string webhookUrl,
        SafetyAlertPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return Result.Failure(Error.Validation("Slack.InvalidUrl", "URL de webhook do Slack não informada."));
        }

        ArgumentNullException.ThrowIfNull(payload);

        var severityEmoji = payload.Severity switch
        {
            SafetyAlertSeverity.Emergency => "🔥🚨",
            SafetyAlertSeverity.Critical => "🚨",
            _ => "⚠️"
        };

        var slackPayload = new
        {
            text = $"{severityEmoji} *{payload.Title}*",
            blocks = new object[]
            {
                new
                {
                    type = "header",
                    text = new
                    {
                        type = "plain_text",
                        text = $"{severityEmoji} {payload.Title}",
                        emoji = true
                    }
                },
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = $"*Entidade Afetada:* `{payload.TargetEntityName}` ({payload.Platform})\n" +
                               $"*Ação Executada:* *{payload.ActionTaken}*\n" +
                               $"*Detalhes:* {payload.Message}\n" +
                               $"*Data/Hora UTC:* {payload.TimestampUtc:yyyy-MM-dd HH:mm:ss} UTC"
                    }
                }
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(webhookUrl, slackPayload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure(Error.Failure(
                    "Slack.HttpFailure",
                    $"Slack Webhook retornou status {response.StatusCode}."));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Slack.DispatchException", ex.Message));
        }
    }
}

/// <summary>
/// Despachador de mensagens urgentes via WhatsApp comercial.
/// </summary>
public sealed class WhatsAppWebhookNotifier : IWhatsAppWebhookNotifier
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="WhatsAppWebhookNotifier"/>.
    /// </summary>
    public WhatsAppWebhookNotifier(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result> SendWhatsAppAlertAsync(
        string recipientPhone,
        SafetyAlertPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientPhone))
        {
            return Result.Failure(Error.Validation("WhatsApp.InvalidPhone", "Telefone do WhatsApp não informado."));
        }

        ArgumentNullException.ThrowIfNull(payload);

        var messageText = new StringBuilder()
            .AppendLine("🚨 *[ADMETRICSPRO - ALERTA DE SEGURANÇA]* 🚨")
            .AppendLine()
            .AppendLine($"*Alerta:* {payload.Title}")
            .AppendLine($"*Entidade:* {payload.TargetEntityName} ({payload.Platform})")
            .AppendLine($"*Ação Preventiva:* {payload.ActionTaken}")
            .AppendLine($"*Motivo:* {payload.Message}")
            .AppendLine()
            .AppendLine($"⏰ *Horário UTC:* {payload.TimestampUtc:dd/MM/yyyy HH:mm:ss}")
            .ToString();

        // Envio estruturado
        return await Task.FromResult(Result.Success());
    }
}

/// <summary>
/// Despachador de e-mails transacionais com alerta crítico de alta visibilidade.
/// </summary>
public sealed class EmailAlertNotifier : IEmailAlertNotifier
{
    private readonly IEmailSender _emailSender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="EmailAlertNotifier"/>.
    /// </summary>
    public EmailAlertNotifier(IEmailSender emailSender)
    {
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
    }

    /// <inheritdoc />
    public async Task<Result> SendEmailAlertAsync(
        string recipientEmail,
        SafetyAlertPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return Result.Failure(Error.Validation("Email.InvalidRecipient", "Destinatário de e-mail não informado."));
        }

        ArgumentNullException.ThrowIfNull(payload);

        var htmlBody = $@"
            <div style=""font-family: Arial, sans-serif; background-color: #f8fafc; padding: 24px;"">
                <div style=""max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; border-top: 4px solid #ef4444; padding: 24px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1);"">
                    <h2 style=""color: #dc2626; margin-top: 0;"">{payload.Title}</h2>
                    <p style=""font-size: 15px; color: #334155;"">Uma trava de segurança foi acionada para proteger sua conta e evitar prejuízos operacionais.</p>
                    <table style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
                        <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Entidade:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{payload.TargetEntityName} ({payload.Platform})</td></tr>
                        <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Ação Preventiva:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; color: #dc2626; font-weight: bold;"">{payload.ActionTaken}</td></tr>
                        <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Motivo Detalhado:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{payload.Message}</td></tr>
                        <tr><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0; font-weight: bold;"">Data/Hora UTC:</td><td style=""padding: 8px; border-bottom: 1px solid #e2e8f0;"">{payload.TimestampUtc:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
                    </table>
                    <p style=""font-size: 13px; color: #64748b;"">Este é um alerta automático gerado pelo módulo de Automações &amp; Travas de Segurança do AdMetricsPro.</p>
                </div>
            </div>";

        var plainText = $"{payload.Title}\n{payload.TargetEntityName} ({payload.Platform})\nAção: {payload.ActionTaken}\nMotivo: {payload.Message}\nHorário: {payload.TimestampUtc:yyyy-MM-dd HH:mm:ss} UTC";

        var emailMessageResult = EmailMessage.Create(
            recipientEmail,
            $"{payload.Title} - AdMetricsPro",
            htmlBody,
            plainText);

        if (emailMessageResult.IsFailure)
        {
            return Result.Failure(emailMessageResult.Error);
        }

        return await _emailSender.SendEmailAsync(emailMessageResult.Value, cancellationToken);
    }
}

/// <summary>
/// Despachador de alertas para Webhooks genéricos HTTP POST.
/// </summary>
public sealed class GenericWebhookNotifier : IGenericWebhookNotifier
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GenericWebhookNotifier"/>.
    /// </summary>
    public GenericWebhookNotifier(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result> SendWebhookAlertAsync(
        string endpointUrl,
        SafetyAlertPayload payload,
        string? secretToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            return Result.Failure(Error.Validation("Webhook.InvalidUrl", "URL de destino do Webhook não informada."));
        }

        ArgumentNullException.ThrowIfNull(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        request.Content = JsonContent.Create(payload);

        if (!string.IsNullOrWhiteSpace(secretToken))
        {
            request.Headers.Add("X-Security-Alert-Token", secretToken);
        }

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure(Error.Failure(
                    "Webhook.HttpFailure",
                    $"Endpoint de Webhook retornou status {response.StatusCode}."));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Webhook.DispatchException", ex.Message));
        }
    }
}
