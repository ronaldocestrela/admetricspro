namespace BuildingBlocks.Domain.Messaging;

/// <summary>
/// Status de uma linha na outbox transacional de eventos de integração.
/// </summary>
public enum IntegrationOutboxMessageStatus
{
    /// <summary>
    /// Aguardando despacho pelo worker.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Reivindicada por um dispatcher em publicação.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Publicada com sucesso no broker.
    /// </summary>
    Published = 2,

    /// <summary>
    /// Esgotou tentativas ou falha classificada como permanente no transporte.
    /// </summary>
    DeadLetter = 3,
}