using System.Text.RegularExpressions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Emails;

/// <summary>
/// Representa uma mensagem de e-mail transacional validada e imutável para envio.
/// </summary>
public sealed partial record EmailMessage
{
    private static readonly Regex EmailFormatRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private EmailMessage(
        string to,
        string subject,
        string htmlBody,
        string plainTextBody,
        string? fromEmail,
        string? fromDisplayName,
        EmailAttachment? attachment)
    {
        To = to;
        Subject = subject;
        HtmlBody = htmlBody;
        PlainTextBody = plainTextBody;
        FromEmail = fromEmail;
        FromDisplayName = fromDisplayName;
        Attachment = attachment;
    }

    /// <summary>
    /// Obtém o endereço de e-mail do destinatário.
    /// </summary>
    public string To { get; }

    /// <summary>
    /// Obtém o assunto da mensagem.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Obtém o corpo da mensagem em formato HTML.
    /// </summary>
    public string HtmlBody { get; }

    /// <summary>
    /// Obtém o corpo alternativo da mensagem em formato texto plano.
    /// </summary>
    public string PlainTextBody { get; }

    /// <summary>
    /// Obtém o endereço de remetente customizado opcional.
    /// </summary>
    public string? FromEmail { get; }

    /// <summary>
    /// Obtém o nome de exibição do remetente opcional.
    /// </summary>
    public string? FromDisplayName { get; }

    /// <summary>
    /// Obtém o anexo opcional (ex.: PDF de relatório).
    /// </summary>
    public EmailAttachment? Attachment { get; }

    /// <summary>
    /// Fábrica para criação e validação de uma nova mensagem de e-mail transacional.
    /// </summary>
    /// <param name="to">Endereço de e-mail do destinatário.</param>
    /// <param name="subject">Assunto da mensagem.</param>
    /// <param name="htmlBody">Corpo principal em HTML.</param>
    /// <param name="plainTextBody">Corpo alternativo em texto plano.</param>
    /// <param name="fromEmail">Endereço de remetente customizado opcional.</param>
    /// <param name="fromDisplayName">Nome de exibição do remetente opcional.</param>
    /// <param name="attachment">Anexo opcional.</param>
    /// <returns>Resultado com a mensagem criada ou erro de validação.</returns>
    public static Result<EmailMessage> Create(
        string to,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        string? fromEmail = null,
        string? fromDisplayName = null,
        EmailAttachment? attachment = null)
    {
        if (string.IsNullOrWhiteSpace(to) || !EmailFormatRegex.IsMatch(to.Trim()))
        {
            return Result<EmailMessage>.Failure(EmailErrors.InvalidRecipient);
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result<EmailMessage>.Failure(EmailErrors.SubjectRequired);
        }

        var normalizedHtml = htmlBody ?? string.Empty;
        var normalizedPlainText = plainTextBody ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedHtml) && string.IsNullOrWhiteSpace(normalizedPlainText))
        {
            return Result<EmailMessage>.Failure(EmailErrors.BodyRequired);
        }

        if (attachment is not null && (attachment.Content is null || attachment.Content.Length == 0))
        {
            return Result<EmailMessage>.Failure(
                Error.Validation("Email.AttachmentEmpty", "O anexo do e-mail não pode estar vazio."));
        }

        var message = new EmailMessage(
            to.Trim().ToLowerInvariant(),
            subject.Trim(),
            normalizedHtml.Trim(),
            string.IsNullOrWhiteSpace(normalizedPlainText) ? normalizedHtml.Trim() : normalizedPlainText.Trim(),
            string.IsNullOrWhiteSpace(fromEmail) ? null : fromEmail.Trim(),
            string.IsNullOrWhiteSpace(fromDisplayName) ? null : fromDisplayName.Trim(),
            attachment);

        return Result<EmailMessage>.Success(message);
    }
}
