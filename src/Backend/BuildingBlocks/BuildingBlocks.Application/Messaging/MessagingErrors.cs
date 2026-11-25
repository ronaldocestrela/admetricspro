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

    /// <summary>
    /// Mensageria ou despacho desligado por configuração.
    /// </summary>
    public static Error Disabled { get; } =
        Error.Failure("Messaging.Disabled", "O despacho de outbox está desabilitado por configuração.");

    /// <summary>
    /// Outra instância detém o lease do scheduler de despacho.
    /// </summary>
    public static Error LeaseNotAcquired { get; } =
        Error.Conflict("Messaging.LeaseNotAcquired", "O lease de despacho de outbox não foi adquirido nesta instância.");

    /// <summary>
    /// Falha ao publicar no broker após claim da linha.
    /// </summary>
    public static Error PublishFailed { get; } =
        Error.Failure("Messaging.PublishFailed", "O broker recusou ou falhou ao publicar a mensagem de integração.");

    /// <summary>
    /// Já existe reprocessamento pendente para o mesmo evento original.
    /// </summary>
    public static Error ReprocessPending { get; } =
        Error.Conflict(
            "Messaging.ReprocessPending",
            "Já existe uma linha de reprocessamento pendente para este evento.");
}