using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.Messaging.Scheduling;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Implementação do lease exclusivo compartilhado pelos workers de scheduler.
/// </summary>
/// <param name="leaseStore">Persistência do lease no MasterDb.</param>
/// <param name="instanceId">Identificador único do processo.</param>
/// <param name="leaseOptions">Duração do lease alinhada ao dispatcher.</param>
public sealed class MessagingSchedulerLeader(
    IMessagingSchedulerLeaseStore leaseStore,
    MessagingHostInstanceId instanceId,
    IOptions<MessagingDispatchLeaseOptions> leaseOptions) : IMessagingSchedulerLeader
{
    /// <inheritdoc />
    public Task<Result> TryLeadCycleAsync(CancellationToken cancellationToken = default) =>
        leaseStore.TryAcquireOrRenewAsync(
            MessagingSchedulerLease.OutboxDispatchLeaseKey,
            instanceId.Value,
            DateTime.UtcNow,
            leaseOptions.Value.LeaseDuration,
            cancellationToken);
}