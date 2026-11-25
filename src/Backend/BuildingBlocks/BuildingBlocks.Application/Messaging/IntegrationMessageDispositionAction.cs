namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Ação que o consumidor deve tomar sobre a mensagem AMQP após processar o comando MediatR.
/// </summary>
public enum IntegrationMessageDispositionAction
{
    /// <summary>
    /// Confirmar processamento (ack) e registrar inbox quando aplicável.
    /// </summary>
    Acknowledge = 0,

    /// <summary>
    /// Reagendar via outbox com <c>NextAttemptAtUtc</c> (não retentar indefinidamente no canal).
    /// </summary>
    Reschedule = 1,

    /// <summary>
    /// Solicitar retry curto no transporte; dead-letter após esgotar tentativas.
    /// </summary>
    RetryTransport = 2,
}