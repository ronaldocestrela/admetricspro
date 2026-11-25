namespace WebApi.Configuration;

/// <summary>
/// Opções do consumidor de sincronização de contas de mídia paga.
/// </summary>
public sealed class MediaSyncConsumerOptions
{
    /// <summary>
    /// Seção <c>Messaging:Consumers:MediaSync</c>.
    /// </summary>
    public const string SectionName = "Messaging:Consumers:MediaSync";

    /// <summary>
    /// Prefetch do endpoint (1 a 4) para respeitar rate limits de API.
    /// </summary>
    public ushort Prefetch { get; set; } = 2;

    /// <summary>
    /// Tentativas máximas de consumo antes de dead-letter.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Atraso padrão em segundos quando a rede retorna rate limit sem Retry-After.
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 60;
}