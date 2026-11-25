using BuildingBlocks.Domain.Primitives;

namespace Integrations.Application.Messaging;

/// <summary>
/// Falha transitória de sincronização de mídia (rate limit, throttling) elegível a reagendamento via outbox.
/// </summary>
public sealed record MediaSyncTransientFailure : Error, ITransientIntegrationFailure
{
    /// <summary>
    /// Inicializa o erro transitório com próxima tentativa sugerida.
    /// </summary>
    /// <param name="sourceCode">Código do erro original.</param>
    /// <param name="details">Detalhes legíveis.</param>
    /// <param name="nextAttemptAtUtc">Instante UTC sugerido para nova tentativa.</param>
    public MediaSyncTransientFailure(string sourceCode, string details, DateTimeOffset nextAttemptAtUtc)
        : base(sourceCode, details, ErrorType.Failure)
    {
        NextAttemptAtUtc = nextAttemptAtUtc;
    }

    /// <inheritdoc />
    public DateTimeOffset? NextAttemptAtUtc { get; }
}