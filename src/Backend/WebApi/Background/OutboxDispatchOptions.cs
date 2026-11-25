namespace WebApi.Background;

/// <summary>
/// Opções do worker de despacho da outbox transacional para RabbitMQ.
/// </summary>
public sealed class OutboxDispatchOptions
{
    /// <summary>
    /// Nome da seção em <c>IConfiguration</c> (<c>Messaging:Dispatch</c>).
    /// </summary>
    public const string SectionName = "Messaging:Dispatch";

    /// <summary>
    /// Habilita o ciclo de despacho; padrão desligado.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Intervalo mínimo entre ciclos, em segundos (mínimo 5).
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Tempo máximo em <see cref="BuildingBlocks.Domain.Messaging.IntegrationOutboxMessageStatus.InProgress"/> antes do reclaim, em segundos.
    /// </summary>
    public int InProgressTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Duração do lease do scheduler no MasterDb, em segundos.
    /// </summary>
    public int LeaseDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Obtém o timeout de processamento como <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan InProgressTimeout => TimeSpan.FromSeconds(Math.Max(30, InProgressTimeoutSeconds));

    /// <summary>
    /// Obtém a duração do lease como <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan LeaseDuration => TimeSpan.FromSeconds(Math.Max(15, LeaseDurationSeconds));
}