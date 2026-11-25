namespace BuildingBlocks.Messaging.Contracts;

/// <summary>
/// Wrapper de transporte com JSON do envelope já commitado na outbox.
/// </summary>
public sealed class IntegrationEventTransportMessage
{
    /// <summary>
    /// Obtém ou define o JSON do <see cref="BuildingBlocks.Application.Messaging.IntegrationEventEnvelope"/>.
    /// </summary>
    public string Json { get; init; } = "{}";

    /// <summary>
    /// Obtém ou define o tipo versionado usado como routing key.
    /// </summary>
    public string MessageType { get; init; } = string.Empty;

    /// <summary>
    /// Obtém ou define o identificador do evento.
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Obtém ou define o tenant operacional quando aplicável.
    /// </summary>
    public Guid? TenantId { get; init; }
}