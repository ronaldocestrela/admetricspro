using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Política pura que traduz <see cref="Result"/> e reentregas em ação sobre a mensagem do broker.
/// </summary>
public sealed record IntegrationResultDisposition
{
    private IntegrationResultDisposition(IntegrationMessageDispositionAction action, DateTimeOffset? nextAttemptAtUtc)
    {
        Action = action;
        NextAttemptAtUtc = nextAttemptAtUtc;
    }

    /// <summary>
    /// Obtém a ação recomendada para o consumidor.
    /// </summary>
    public IntegrationMessageDispositionAction Action { get; }

    /// <summary>
    /// Obtém o instante de reagendamento quando <see cref="Action"/> é <see cref="IntegrationMessageDispositionAction.Reschedule"/>.
    /// </summary>
    public DateTimeOffset? NextAttemptAtUtc { get; }

    /// <summary>
    /// Calcula a disposição a partir do resultado do handler MediatR.
    /// </summary>
    /// <param name="result">Resultado do comando já executado.</param>
    /// <param name="alreadyInInbox">Indica se o <c>EventId</c> já foi processado por este consumidor.</param>
    /// <returns>Disposição sem efeitos colaterais.</returns>
    public static IntegrationResultDisposition ForResult(Result result, bool alreadyInInbox)
    {
        if (alreadyInInbox)
        {
            return Acknowledge();
        }

        if (result.IsSuccess)
        {
            return Acknowledge();
        }

        if (IsPermanentBusinessError(result.Error.Type))
        {
            return Acknowledge();
        }

        if (result.Error is ITransientIntegrationFailure transient)
        {
            var nextAttempt = transient.NextAttemptAtUtc ?? DateTimeOffset.UtcNow;
            return new IntegrationResultDisposition(IntegrationMessageDispositionAction.Reschedule, nextAttempt);
        }

        return Acknowledge();
    }

    /// <summary>
    /// Calcula a disposição para exceções não tratadas no consumidor.
    /// </summary>
    /// <returns>Disposição que solicita retry no transporte.</returns>
    public static IntegrationResultDisposition ForUnexpectedFailure() =>
        new(IntegrationMessageDispositionAction.RetryTransport, null);

    private static IntegrationResultDisposition Acknowledge() =>
        new(IntegrationMessageDispositionAction.Acknowledge, null);

    private static bool IsPermanentBusinessError(ErrorType type) =>
        type is ErrorType.Validation
            or ErrorType.NotFound
            or ErrorType.Conflict
            or ErrorType.Unauthorized
            or ErrorType.Forbidden;
}