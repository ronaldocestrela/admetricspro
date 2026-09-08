using System.Text.RegularExpressions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace Master.Domain.Tenants;

/// <summary>
/// Entidade de auditoria e controle de idempotência para notificações transacionais emitidas pelo catálogo Master.
/// </summary>
public sealed class TenantNotificationLog : Entity<Guid>
{
    private static readonly Regex EmailFormatRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private TenantNotificationLog(
        Guid id,
        TenantId tenantId,
        TrialNoticeType type,
        string recipientEmail,
        string subject,
        DateTime sentAtUtc,
        bool isSuccess,
        string? errorMessage)
        : base(id)
    {
        TenantId = tenantId;
        Type = type;
        RecipientEmail = recipientEmail;
        Subject = subject;
        SentAtUtc = sentAtUtc;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    private TenantNotificationLog()
        : base(Guid.NewGuid())
    {
        TenantId = new TenantId(Guid.Empty);
        RecipientEmail = string.Empty;
        Subject = string.Empty;
        SentAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Identificador do tenant destinatário da notificação.
    /// </summary>
    public TenantId TenantId { get; private set; }

    /// <summary>
    /// Tipo da notificação enviada.
    /// </summary>
    public TrialNoticeType Type { get; private set; }

    /// <summary>
    /// Endereço de e-mail do destinatário.
    /// </summary>
    public string RecipientEmail { get; private set; }

    /// <summary>
    /// Assunto da mensagem enviada.
    /// </summary>
    public string Subject { get; private set; }

    /// <summary>
    /// Data e hora de envio em UTC.
    /// </summary>
    public DateTime SentAtUtc { get; private set; }

    /// <summary>
    /// Indica se o envio foi concluído com sucesso.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Mensagem de erro em caso de falha técnica no envio.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Cria uma nova entrada de log de notificação transacional.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="type">Tipo da notificação.</param>
    /// <param name="recipientEmail">Endereço de e-mail de destino.</param>
    /// <param name="subject">Assunto da mensagem.</param>
    /// <param name="isSuccess">Indicador de sucesso do envio.</param>
    /// <param name="errorMessage">Mensagem de erro em caso de falha.</param>
    /// <param name="sentAtUtc">Timestamp de envio opcional (padrão DateTime.UtcNow).</param>
    /// <returns>Resultado contendo a entidade ou erro de validação.</returns>
    public static Result<TenantNotificationLog> Create(
        TenantId tenantId,
        TrialNoticeType type,
        string recipientEmail,
        string subject,
        bool isSuccess,
        string? errorMessage = null,
        DateTime? sentAtUtc = null)
    {
        if (tenantId is null || tenantId.Value == Guid.Empty)
        {
            return Result<TenantNotificationLog>.Failure(
                Error.Validation("NotificationLog.TenantIdRequired", "TenantId é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(recipientEmail) || !EmailFormatRegex.IsMatch(recipientEmail.Trim()))
        {
            return Result<TenantNotificationLog>.Failure(
                Error.Validation("NotificationLog.InvalidRecipientEmail", "E-mail de destinatário inválido."));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result<TenantNotificationLog>.Failure(
                Error.Validation("NotificationLog.SubjectRequired", "Assunto da notificação é obrigatório."));
        }

        var log = new TenantNotificationLog(
            Guid.NewGuid(),
            tenantId,
            type,
            recipientEmail.Trim().ToLowerInvariant(),
            subject.Trim(),
            sentAtUtc ?? DateTime.UtcNow,
            isSuccess,
            errorMessage?.Trim());

        return Result<TenantNotificationLog>.Success(log);
    }
}
