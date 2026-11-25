using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Messaging;

/// <summary>
/// Erros estáveis da entidade de inbox de integração.
/// </summary>
public static class IntegrationInboxErrors
{
    /// <summary>
    /// EventId ausente ou inválido.
    /// </summary>
    public static Error InvalidEventId { get; } =
        Error.Validation("Messaging.PayloadInvalid", "O EventId da inbox deve ser um GUID não vazio.");

    /// <summary>
    /// Nome de consumidor ausente ou em branco.
    /// </summary>
    public static Error InvalidConsumerName { get; } =
        Error.Validation("Messaging.PayloadInvalid", "O nome do consumidor não pode ser vazio.");
}