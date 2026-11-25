namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Porta de leitura da feature flag <c>Messaging:Enabled</c> sem referência ao transporte.
/// </summary>
public interface IMessagingFeatureGate
{
    /// <summary>
    /// Indica se eventos de integração devem usar outbox e fila em vez de handlers in-process.
    /// </summary>
    bool IsEnabled { get; }
}