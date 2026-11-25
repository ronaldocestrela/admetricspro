namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Identificador estável do processo para leases de mensageria.
/// </summary>
public sealed class MessagingHostInstanceId
{
    /// <summary>
    /// Inicializa o identificador único do host.
    /// </summary>
    public MessagingHostInstanceId()
    {
        Value = $"{Environment.MachineName}:{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Obtém o valor do identificador.
    /// </summary>
    public string Value { get; }
}