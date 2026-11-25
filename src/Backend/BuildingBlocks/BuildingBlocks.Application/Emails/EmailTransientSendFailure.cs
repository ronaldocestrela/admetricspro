using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Emails;

/// <summary>
/// Falha transitória de SMTP após esgotar o retry curto no sender; elegível a reagendamento via outbox.
/// </summary>
public sealed record EmailTransientSendFailure : Error, ITransientIntegrationFailure
{
    /// <summary>
    /// Inicializa o erro transitório com próxima tentativa sugerida.
    /// </summary>
    /// <param name="details">Detalhes da falha.</param>
    /// <param name="nextAttemptAtUtc">Instante UTC sugerido para nova tentativa.</param>
    public EmailTransientSendFailure(string details, DateTimeOffset nextAttemptAtUtc)
        : base("Email.TransientSendFailed", $"Falha transitória ao enviar e-mail: {details}", ErrorType.Failure)
    {
        NextAttemptAtUtc = nextAttemptAtUtc;
    }

    /// <inheritdoc />
    public DateTimeOffset? NextAttemptAtUtc { get; }
}