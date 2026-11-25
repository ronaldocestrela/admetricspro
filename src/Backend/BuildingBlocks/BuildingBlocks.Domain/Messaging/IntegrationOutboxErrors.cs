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

    /// <summary>
    /// Linha não está em estado que permita claim.
    /// </summary>
    public static Error NotClaimable { get; } =
        Error.Conflict("Messaging.OutboxNotClaimable", "A linha de outbox não pode ser reivindicada no estado atual.");

    /// <summary>
    /// Transição de status inválida para a operação solicitada.
    /// </summary>
    public static Error InvalidTransition { get; } =
        Error.Validation("Messaging.OutboxInvalidTransition", "Transição de status da outbox inválida.");

    /// <summary>
    /// Instant não está em UTC.
    /// </summary>
    public static Error InstantNotUtc { get; } =
        Error.Validation("Messaging.InstantNotUtc", "O instante deve estar em UTC.");
}