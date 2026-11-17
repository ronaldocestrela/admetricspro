using BuildingBlocks.Application.Emails;
using BuildingBlocks.Domain.Primitives;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;

namespace BuildingBlocks.Infrastructure.Emails;

/// <summary>
/// Provedor SMTP da plataforma que valida configuração, monta MIME e delega entrega com retry transitório.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IOptions<EmailOptions> _options;
    private readonly ISmtpMailTransport _transport;
    private readonly ILogger<SmtpEmailSender> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SmtpEmailSender"/>.
    /// </summary>
    /// <param name="options">Opções de configuração de SMTP.</param>
    /// <param name="transport">Transporte de baixo nível (MailKit em produção).</param>
    /// <param name="logger">Logger estruturado.</param>
    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        ISmtpMailTransport transport,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
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
        var validation = ValidateConfiguration(config);
        if (validation.IsFailure)
        {
            return validation;
        }

        var secureSocket = ResolveSecureSocket(config);
        var senderEmail = !string.IsNullOrWhiteSpace(message.FromEmail)
            ? message.FromEmail
            : config.FromEmail;
        var senderDisplayName = !string.IsNullOrWhiteSpace(message.FromDisplayName)
            ? message.FromDisplayName
            : config.FromDisplayName;

        var mimeMessage = BuildMimeMessage(message, senderEmail, senderDisplayName);
        var userName = string.IsNullOrWhiteSpace(config.UserName) ? null : config.UserName.Trim();
        var password = string.IsNullOrWhiteSpace(config.Password) ? null : config.Password;

        var request = new SmtpMailSendRequest(
            config.SmtpHost.Trim(),
            config.SmtpPort,
            secureSocket,
            config.TimeoutSeconds <= 0 ? 10 : config.TimeoutSeconds,
            userName,
            password,
            mimeMessage);

        var maxAttempts = 1 + Math.Max(0, config.MaxRetryAttempts);
        string? lastError = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Enviando e-mail transacional via SMTP para {To} através do host {Host}:{Port} (tentativa {Attempt}/{MaxAttempts})...",
                    message.To,
                    request.Host,
                    request.Port,
                    attempt,
                    maxAttempts);

                await _transport.SendAsync(request, cancellationToken);

                _logger.LogInformation("E-mail transacional para {To} enviado com sucesso via SMTP.", message.To);
                return Result.Success();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SmtpDeliveryException ex) when (ex.IsTransient && attempt < maxAttempts)
            {
                lastError = ex.Message;
                _logger.LogWarning(
                    ex,
                    "Falha transitória ao enviar e-mail para {To}; nova tentativa {NextAttempt}/{MaxAttempts}.",
                    message.To,
                    attempt + 1,
                    maxAttempts);
            }
            catch (SmtpDeliveryException ex)
            {
                lastError = ex.Message;
                _logger.LogError(ex, "Falha ao enviar e-mail transacional via SMTP para {To}.", message.To);
                return Result.Failure(EmailErrors.SendFailed(ex.Message));
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogError(ex, "Falha inesperada ao enviar e-mail transacional via SMTP para {To}.", message.To);
                return Result.Failure(EmailErrors.SendFailed(ex.Message));
            }
        }

        return Result.Failure(EmailErrors.SendFailed(lastError ?? "Falha transitória após esgotar tentativas."));
    }

    /// <summary>
    /// Valida opções obrigatórias do SMTP da plataforma.
    /// </summary>
    internal static Result ValidateConfiguration(EmailOptions config)
    {
        if (config is null)
        {
            return Result.Failure(EmailErrors.InvalidConfiguration);
        }

        if (string.IsNullOrWhiteSpace(config.SmtpHost))
        {
            return Result.Failure(EmailErrors.InvalidConfiguration);
        }

        if (config.SmtpPort is <= 0 or > 65535)
        {
            return Result.Failure(EmailErrors.InvalidConfiguration);
        }

        if (string.IsNullOrWhiteSpace(config.FromEmail))
        {
            return Result.Failure(EmailErrors.InvalidConfiguration);
        }

        var hasUser = !string.IsNullOrWhiteSpace(config.UserName);
        var hasPassword = !string.IsNullOrWhiteSpace(config.Password);
        if (hasUser != hasPassword)
        {
            return Result.Failure(EmailErrors.InvalidConfiguration);
        }

        return Result.Success();
    }

    /// <summary>
    /// Resolve o modo TLS efetivo a partir das opções configuradas.
    /// </summary>
    internal static SecureSocketOptions ResolveSecureSocket(EmailOptions config)
    {
        if (config.SecureSocket.HasValue)
        {
            return config.SecureSocket.Value switch
            {
                EmailSecureSocketMode.Auto => SecureSocketOptions.Auto,
                EmailSecureSocketMode.None => SecureSocketOptions.None,
                EmailSecureSocketMode.StartTls => SecureSocketOptions.StartTls,
                EmailSecureSocketMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
                _ => SecureSocketOptions.Auto
            };
        }

        if (!config.EnableSsl)
        {
            return SecureSocketOptions.None;
        }

        return config.SmtpPort == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }

    private static MimeMessage BuildMimeMessage(EmailMessage message, string senderEmail, string senderDisplayName)
    {
        var mimeMessage = new MimeMessage
        {
            Subject = message.Subject
        };

        mimeMessage.From.Add(new MailboxAddress(senderDisplayName, senderEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.MessageId = MimeUtils.GenerateMessageId();

        var multipart = new MultipartAlternative();

        if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
        {
            multipart.Add(new TextPart("plain") { Text = message.PlainTextBody });
        }

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            multipart.Add(new TextPart("html") { Text = message.HtmlBody });
        }

        mimeMessage.Body = multipart;
        return mimeMessage;
    }
}