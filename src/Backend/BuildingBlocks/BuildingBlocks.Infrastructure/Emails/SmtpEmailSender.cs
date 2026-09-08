using System.Net;
using System.Net.Mail;
using BuildingBlocks.Application.Emails;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Emails;

/// <summary>
/// Provedor SMTP padrão para envio assíncrono de e-mails transacionais.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SmtpEmailSender"/>.
    /// </summary>
    /// <param name="options">Opções de configuração de SMTP.</param>
    /// <param name="logger">Logger estruturado.</param>
    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (message is null)
        {
            return Result.Failure(EmailErrors.InvalidRecipient);
        }

        var config = _options.Value;

        try
        {
            var senderEmail = !string.IsNullOrWhiteSpace(message.FromEmail)
                ? message.FromEmail
                : config.FromEmail;

            var senderDisplayName = !string.IsNullOrWhiteSpace(message.FromDisplayName)
                ? message.FromDisplayName
                : config.FromDisplayName;

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderDisplayName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(message.To));

            if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
            {
                var plainTextView = AlternateView.CreateAlternateViewFromString(
                    message.PlainTextBody,
                    null,
                    "text/plain");
                mailMessage.AlternateViews.Add(plainTextView);
            }

            using var client = new SmtpClient(config.SmtpHost, config.SmtpPort)
            {
                EnableSsl = config.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(config.UserName) && !string.IsNullOrWhiteSpace(config.Password))
            {
                client.Credentials = new NetworkCredential(config.UserName, config.Password);
            }

            _logger.LogInformation(
                "Enviando e-mail transacional via SMTP para {To} através do host {Host}:{Port}...",
                message.To,
                config.SmtpHost,
                config.SmtpPort);

            await client.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogInformation("E-mail transacional para {To} enviado com sucesso via SMTP.", message.To);
            return Result.Success();
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail transacional via SMTP para {To}.", message.To);
            return Result.Failure(EmailErrors.SendFailed(ex.Message));
        }
    }
}
