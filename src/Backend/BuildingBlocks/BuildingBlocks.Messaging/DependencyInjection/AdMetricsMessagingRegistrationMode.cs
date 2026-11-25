namespace BuildingBlocks.Messaging.DependencyInjection;

/// <summary>
/// Modo de registro MassTransit conforme o papel do host (Fase 10).
/// </summary>
public enum AdMetricsMessagingRegistrationMode
{
    /// <summary>
    /// Sem MassTransit; publicador nulo.
    /// </summary>
    None,

    /// <summary>
    /// Somente publicação no exchange (scheduler).
    /// </summary>
    PublisherOnly,

    /// <summary>
    /// Somente consumidores e filas.
    /// </summary>
    ConsumersOnly,

    /// <summary>
    /// Publicação e consumo (instância <c>All</c>).
    /// </summary>
    Full,
}

/// <summary>
/// Resolve o modo de registro a partir da feature flag e do papel.
/// </summary>
public static class AdMetricsMessagingRegistrationModeResolver
{
    /// <summary>
    /// Determina o modo efetivo de transporte.
    /// </summary>
    /// <param name="messagingEnabled">Valor de <c>Messaging:Enabled</c>.</param>
    /// <param name="hostRole">Papel do host já validado.</param>
    /// <returns>Modo de registro MassTransit.</returns>
    public static AdMetricsMessagingRegistrationMode Resolve(bool messagingEnabled, BuildingBlocks.Application.Messaging.MessagingHostRole hostRole)
    {
        if (!messagingEnabled)
        {
            return AdMetricsMessagingRegistrationMode.None;
        }

        return hostRole switch
        {
            BuildingBlocks.Application.Messaging.MessagingHostRole.Api => AdMetricsMessagingRegistrationMode.None,
            BuildingBlocks.Application.Messaging.MessagingHostRole.Scheduler => AdMetricsMessagingRegistrationMode.PublisherOnly,
            BuildingBlocks.Application.Messaging.MessagingHostRole.Consumer => AdMetricsMessagingRegistrationMode.ConsumersOnly,
            BuildingBlocks.Application.Messaging.MessagingHostRole.All => AdMetricsMessagingRegistrationMode.Full,
            _ => AdMetricsMessagingRegistrationMode.None,
        };
    }
}