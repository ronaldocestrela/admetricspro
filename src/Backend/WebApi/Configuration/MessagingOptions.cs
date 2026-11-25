namespace WebApi.Configuration;

/// <summary>
/// Opções de feature flag e papel do host para mensageria durável (Fase 1+).
/// </summary>
public sealed class MessagingOptions
{
    /// <summary>
    /// Nome da seção em <c>IConfiguration</c> (variável de ambiente <c>Messaging__*</c>).
    /// </summary>
    public const string SectionName = "Messaging";

    /// <summary>
    /// Quando <see langword="true"/>, registra health check do broker e habilita integrações de mensageria nas fases seguintes.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Papel do processo: <c>All</c>, <c>Api</c>, <c>Scheduler</c> ou <c>Consumer</c> (efetivo a partir da Fase 10).
    /// </summary>
    public string HostRole { get; set; } = "All";
}