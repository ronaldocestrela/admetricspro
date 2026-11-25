using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Messaging;

/// <summary>
/// Aplica o modelo de mensageria transacional a um <see cref="ModelBuilder"/>.
/// </summary>
public static class MessagingModelBuilderExtensions
{
    /// <summary>
    /// Registra entidades de outbox, inbox e opcionalmente lease de scheduler.
    /// </summary>
    /// <param name="modelBuilder">Builder do modelo.</param>
    /// <param name="includeSchedulerLease">Quando <see langword="true"/>, inclui lease no MasterDb.</param>
    public static void ApplyMessagingModel(this ModelBuilder modelBuilder, bool includeSchedulerLease)
    {
        modelBuilder.ApplyConfiguration(new IntegrationOutboxMessageEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new IntegrationInboxMessageEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserInitiatedJobEntityTypeConfiguration());

        if (includeSchedulerLease)
        {
            modelBuilder.ApplyConfiguration(new MessagingSchedulerLeaseEntityTypeConfiguration());
        }
    }
}