using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Primitives;

namespace IntegrationTests.Messaging.Laboratory;

/// <summary>
/// Lease que sempre adquire, para testes de integração sem disputa entre workers.
/// </summary>
public sealed class TestMessagingSchedulerLeaseStore : IMessagingSchedulerLeaseStore
{
    /// <inheritdoc />
    public Task<Result> TryAcquireOrRenewAsync(
        string leaseKey,
        string holderInstanceId,
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success());
}