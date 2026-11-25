namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Marca um erro de integração como falha transitória que deve reagendar a outbox em vez de retry no transporte.
/// </summary>
public interface ITransientIntegrationFailure
{
    /// <summary>
    /// Obtém o instante UTC sugerido para a próxima tentativa, quando conhecido.
    /// </summary>
    DateTimeOffset? NextAttemptAtUtc { get; }
}