namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Contrato de um fato de negócio já commitado destinado a transporte durável (outbox / fila).
/// Não substitui eventos de domínio in-process.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Obtém o identificador único do evento (<c>message-id</c> no broker).
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Obtém o tipo versionado (routing key lógica), ex.: <c>integrations.campaign-metrics-synced.v1</c>.
    /// </summary>
    string MessageType { get; }

    /// <summary>
    /// Obtém o identificador de correlação do fluxo causador.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// Obtém o instante UTC em que o fato ocorreu.
    /// </summary>
    DateTimeOffset OccurredOnUtc { get; }

    /// <summary>
    /// Obtém o escopo de persistência (catálogo ou operacional).
    /// </summary>
    IntegrationEventScope Scope { get; }

    /// <summary>
    /// Obtém o tenant quando aplicável; obrigatório para escopo operacional.
    /// </summary>
    Guid? TenantId { get; }
}