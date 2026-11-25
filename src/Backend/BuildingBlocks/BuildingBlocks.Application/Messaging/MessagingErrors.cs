using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Códigos estáveis de falha do módulo de mensageria de integração.
/// </summary>
public static class MessagingErrors
{
    /// <summary>
    /// Envelope inválido (EventId, tipo, correlação ou payload incompatível).
    /// </summary>
    public static Error PayloadInvalid { get; } =
        Error.Validation("Messaging.PayloadInvalid", "O envelope de integração é inválido ou incompatível com a versão esperada.");

    /// <summary>
    /// Evento operacional publicado sem <c>TenantId</c>.
    /// </summary>
    public static Error TenantRequired { get; } =
        Error.Validation("Messaging.TenantRequired", "Eventos operacionais exigem TenantId no envelope.");
}