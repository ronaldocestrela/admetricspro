using Automations.Domain.SafetyGuards;
using BuildingBlocks.Domain.Automations.SafetyGuards;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;

namespace Automations.Infrastructure.SafetyGuards;

/// <summary>
/// Despachador coordenador de alertas de segurança multi-canal (Slack, WhatsApp, E-mail, Webhook).
/// Garante entrega simultânea com isolamento de falhas parciais entre canais.
/// </summary>
public sealed class SecurityAlertDispatcher : ISecurityAlertNotifier
{
    private readonly ISlackWebhookNotifier _slackNotifier;
    private readonly IWhatsAppWebhookNotifier _whatsappNotifier;
    private readonly IEmailAlertNotifier _emailNotifier;
    private readonly IGenericWebhookNotifier _webhookNotifier;
    private readonly SecurityAlertNotificationOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SecurityAlertDispatcher"/> com opções tipadas.
    /// </summary>
    public SecurityAlertDispatcher(
        ISlackWebhookNotifier slackNotifier,
        IWhatsAppWebhookNotifier whatsappNotifier,
        IEmailAlertNotifier emailNotifier,
        IGenericWebhookNotifier webhookNotifier,
        IOptions<SecurityAlertNotificationOptions> options)
        : this(slackNotifier, whatsappNotifier, emailNotifier, webhookNotifier, options?.Value ?? new SecurityAlertNotificationOptions())
    {
    }

    /// <summary>
    /// Construtor interno para testes unitários com opções diretas.
    /// </summary>
    public SecurityAlertDispatcher(
        ISlackWebhookNotifier slackNotifier,
        IWhatsAppWebhookNotifier whatsappNotifier,
        IEmailAlertNotifier emailNotifier,
        IGenericWebhookNotifier webhookNotifier,
        SecurityAlertNotificationOptions options)
    {
        _slackNotifier = slackNotifier ?? throw new ArgumentNullException(nameof(slackNotifier));
        _whatsappNotifier = whatsappNotifier ?? throw new ArgumentNullException(nameof(whatsappNotifier));
        _emailNotifier = emailNotifier ?? throw new ArgumentNullException(nameof(emailNotifier));
        _webhookNotifier = webhookNotifier ?? throw new ArgumentNullException(nameof(webhookNotifier));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyDictionary<SafetyAlertChannel, bool>>> DispatchAlertAsync(
        SafetyAlertPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var tasks = new List<Task<(SafetyAlertChannel Channel, bool Succeeded)>>();

        // 1. Canal Slack
        if (!string.IsNullOrWhiteSpace(_options.SlackWebhookUrl))
        {
            tasks.Add(SendSlackAsync(_options.SlackWebhookUrl, payload, cancellationToken));
        }

        // 2. Canal WhatsApp
        if (!string.IsNullOrWhiteSpace(_options.WhatsAppRecipientPhone))
        {
            tasks.Add(SendWhatsAppAsync(_options.WhatsAppRecipientPhone, payload, cancellationToken));
        }

        // 3. Canal E-mail
        if (!string.IsNullOrWhiteSpace(_options.AlertRecipientEmail))
        {
            tasks.Add(SendEmailAsync(_options.AlertRecipientEmail, payload, cancellationToken));
        }

        // 4. Canal Webhook Genérico
        if (!string.IsNullOrWhiteSpace(_options.GenericWebhookUrl))
        {
            tasks.Add(SendWebhookAsync(_options.GenericWebhookUrl, payload, _options.GenericWebhookSecret, cancellationToken));
        }

        if (tasks.Count == 0)
        {
            return Result<IReadOnlyDictionary<SafetyAlertChannel, bool>>.Success(
                new Dictionary<SafetyAlertChannel, bool>());
        }

        var results = await Task.WhenAll(tasks);
        var report = results.ToDictionary(r => r.Channel, r => r.Succeeded);

        return Result<IReadOnlyDictionary<SafetyAlertChannel, bool>>.Success(report);
    }

    private async Task<(SafetyAlertChannel Channel, bool Succeeded)> SendSlackAsync(
        string url, SafetyAlertPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _slackNotifier.SendSlackAlertAsync(url, payload, cancellationToken);
            return (SafetyAlertChannel.Slack, result.IsSuccess);
        }
        catch
        {
            return (SafetyAlertChannel.Slack, false);
        }
    }

    private async Task<(SafetyAlertChannel Channel, bool Succeeded)> SendWhatsAppAsync(
        string phone, SafetyAlertPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _whatsappNotifier.SendWhatsAppAlertAsync(phone, payload, cancellationToken);
            return (SafetyAlertChannel.WhatsApp, result.IsSuccess);
        }
        catch
        {
            return (SafetyAlertChannel.WhatsApp, false);
        }
    }

    private async Task<(SafetyAlertChannel Channel, bool Succeeded)> SendEmailAsync(
        string email, SafetyAlertPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _emailNotifier.SendEmailAlertAsync(email, payload, cancellationToken);
            return (SafetyAlertChannel.Email, result.IsSuccess);
        }
        catch
        {
            return (SafetyAlertChannel.Email, false);
        }
    }

    private async Task<(SafetyAlertChannel Channel, bool Succeeded)> SendWebhookAsync(
        string url, SafetyAlertPayload payload, string? secret, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _webhookNotifier.SendWebhookAlertAsync(url, payload, secret, cancellationToken);
            return (SafetyAlertChannel.Webhook, result.IsSuccess);
        }
        catch
        {
            return (SafetyAlertChannel.Webhook, false);
        }
    }
}
