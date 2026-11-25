using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.Commands.SyncCampaignHierarchy;
using Integrations.Application.Campaigns.Commands.SyncCampaignMetrics;
using Integrations.Application.Persistence;
using Integrations.Domain.Meta;
using MediatR;
using Microsoft.Extensions.Options;
using Tenants.Application.Integrations.DTOs;

namespace WebApi.Background;

/// <summary>
/// Sincronização Meta in-process com journal e métricas no lookback configurado.
/// </summary>
public sealed class ConnectedAdAccountMetaSyncExecutor(
    ISender sender,
    IMetaOperationRepository journal,
    IIntegrationsUnitOfWork unitOfWork,
    IOptions<MetaSynchronizationOptions> options,
    ILogger<ConnectedAdAccountMetaSyncExecutor> logger) : IConnectedAdAccountMetaSyncExecutor
{
    private static readonly Guid SystemActorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly System.Diagnostics.Metrics.Meter Meter = new("AdMetricsPro.MetaSynchronization");
    private static readonly System.Diagnostics.Metrics.Counter<long> Runs = Meter.CreateCounter<long>("admetrics.meta.sync.runs");

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(Guid tenantId, ConnectedAdAccountDto account, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || account.IsDemo || account.Platform != "MetaAds" || account.Status != "Connected")
        {
            return Result.Failure(Error.Validation("MetaSync.IneligibleAccount", "Conta inelegível para sincronização Meta."));
        }

        var operationId = Guid.NewGuid();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{tenantId}:{account.WorkspaceId}:{account.Id}:{operationId}")));
        var entry = MetaApiOperation.Create(operationId, account.WorkspaceId, account.Id, SystemActorId, "sync", hash).Value;
        var claim = await journal.TryBeginAsync(entry, cancellationToken);
        if (claim.IsFailure)
        {
            return Result.Failure(claim.Error);
        }

        if (!claim.Value)
        {
            return Result.Success();
        }

        var hierarchy = await sender.Send(new SyncCampaignHierarchyCommand(account.WorkspaceId, account.Id, "MetaAds"), cancellationToken);
        Error? syncError = hierarchy.IsFailure ? hierarchy.Error : null;
        if (syncError is null)
        {
            var timeZone = TimeZoneInfo.Utc;
            if (account.TimeZone is { } name && TimeZoneInfo.TryFindSystemTimeZoneById(name, out var knownTimeZone))
            {
                timeZone = knownTimeZone;
            }

            var until = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date, DateTimeKind.Utc);
            var lookbackDays = Math.Clamp(options.Value.LookbackDays, 1, 90);
            var metrics = await sender.Send(
                new SyncCampaignMetricsCommand(
                    account.WorkspaceId,
                    account.Id,
                    "MetaAds",
                    until.AddDays(-lookbackDays),
                    until,
                    MetricGranularity.Daily),
                cancellationToken);
            if (metrics.IsFailure)
            {
                syncError = metrics.Error;
            }
        }

        var failureCode = syncError?.Code;
        if (syncError is not null)
        {
            entry.Complete(account.ExternalAccountId, failureCode);
            journal.Update(entry);
            await unitOfWork.CommitAsync(cancellationToken);
            Runs.Add(1, new KeyValuePair<string, object?>("state", entry.State));
            return Result.Failure(syncError);
        }

        entry.Complete(account.ExternalAccountId, null);
        journal.Update(entry);
        await unitOfWork.CommitAsync(cancellationToken);
        Runs.Add(1, new KeyValuePair<string, object?>("state", entry.State));
        logger.LogInformation(
            "Sincronização Meta {OperationId}: tenant {TenantId}, workspace {WorkspaceId}, conta {AccountId}, estado {State}",
            operationId,
            tenantId,
            account.WorkspaceId,
            account.Id,
            entry.State);
        return Result.Success();
    }
}