using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Messaging;

/// <summary>
/// Erros estáveis da entidade de outbox de integração.
/// </summary>
public static class IntegrationOutboxErrors
{
    /// <summary>
    /// EventId ausente ou inválido para persistência na outbox.
    /// </summary>
    public static Error InvalidEventId { get; } =
        Error.Validation("Messaging.PayloadInvalid", "O EventId da outbox deve ser um GUID não vazio.");

    /// <summary>
    /// Tipo de mensagem ausente ou em branco.
    /// </summary>
    public static Error InvalidMessageType { get; } =
        Error.Validation("Messaging.PayloadInvalid", "O tipo da mensagem de integração não pode ser vazio.");
}