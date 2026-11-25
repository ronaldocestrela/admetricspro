namespace WebApi.Configuration;

/// <summary>
/// Opções compartilhadas dos consumidores de reação pós-ingestão (Fase 6).
/// </summary>
public sealed class IngestionReactionsConsumerOptions
{
    /// <summary>
    /// Seção <c>Messaging:Consumers:IngestionReactions</c>.
    /// </summary>
    public const string SectionName = "Messaging:Consumers:IngestionReactions";

    /// <summary>
    /// Prefetch do endpoint (1 a 4).
    /// </summary>
    public ushort Prefetch { get; set; } = 2;

    /// <summary>
    /// Tentativas máximas antes de dead-letter na outbox do tenant.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Atraso padrão em segundos para reagendamento transitório.
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 60;
}