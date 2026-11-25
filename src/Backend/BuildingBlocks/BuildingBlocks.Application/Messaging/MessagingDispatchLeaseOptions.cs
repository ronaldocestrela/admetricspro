namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Opções de lease do scheduler compartilhadas pelos laços temporais (seção <c>Messaging:Dispatch</c>).
/// </summary>
public sealed class MessagingDispatchLeaseOptions
{
    /// <summary>
    /// Nome da seção em <c>IConfiguration</c>.
    /// </summary>
    public const string SectionName = "Messaging:Dispatch";

    /// <summary>
    /// Duração do lease no MasterDb, em segundos.
    /// </summary>
    public int LeaseDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Obtém a duração do lease como <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan LeaseDuration => TimeSpan.FromSeconds(Math.Max(15, LeaseDurationSeconds));
}