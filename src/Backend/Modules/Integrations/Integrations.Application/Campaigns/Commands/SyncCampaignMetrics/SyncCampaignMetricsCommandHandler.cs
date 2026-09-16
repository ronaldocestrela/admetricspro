using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Campaigns.Events;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Campaigns.Sync;
using Integrations.Domain.OAuth;
using MediatR;

namespace Integrations.Application.Campaigns.Commands.SyncCampaignMetrics;

/// <summary>
/// Manipulador do comando <see cref="SyncCampaignMetricsCommand"/>.
/// Orquestra a extração analítica por rede, mapeamento relacional com entidades de campanha,
/// persistência atômica idempotente e emissão do evento in-memory <see cref="CampaignMetricsSyncedEvent"/>.
/// </summary>
public sealed class SyncCampaignMetricsCommandHandler : ICommandHandler<SyncCampaignMetricsCommand, SyncCampaignMetricsSummaryDto>
{
    private readonly ICampaignMetricsRepository _metricsRepository;
    private readonly ICampaignHierarchyRepository _hierarchyRepository;
    private readonly IOAuthTokenVaultRepository _tokenVaultRepository;
    private readonly IOAuthEncryptionService _encryptionService;
    private readonly ICampaignMetricsSyncDispatcher _syncDispatcher;
    private readonly IIntegrationsUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SyncCampaignMetricsCommandHandler"/>.
    /// </summary>
    public SyncCampaignMetricsCommandHandler(
        ICampaignMetricsRepository metricsRepository,
        ICampaignHierarchyRepository hierarchyRepository,
        IOAuthTokenVaultRepository tokenVaultRepository,
        IOAuthEncryptionService encryptionService,
        ICampaignMetricsSyncDispatcher syncDispatcher,
        IIntegrationsUnitOfWork unitOfWork,
        IPublisher publisher,
        ITenantContextAccessor tenantContextAccessor)
    {
        _metricsRepository = metricsRepository ?? throw new ArgumentNullException(nameof(metricsRepository));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _tokenVaultRepository = tokenVaultRepository ?? throw new ArgumentNullException(nameof(tokenVaultRepository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _syncDispatcher = syncDispatcher ?? throw new ArgumentNullException(nameof(syncDispatcher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<SyncCampaignMetricsSummaryDto>> Handle(
        SyncCampaignMetricsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<SyncCampaignMetricsSummaryDto>.Failure(
                Error.Validation("SyncCampaignMetrics.EmptyWorkspaceId", "O workspace é obrigatório para a sincronização de métricas."));
        }

        if (request.StartDateUtc > request.EndDateUtc)
        {
            return Result<SyncCampaignMetricsSummaryDto>.Failure(
                Error.Validation("SyncCampaignMetrics.InvalidDateRange", "A data inicial não pode ser superior à data final."));
        }

        var accounts = await _hierarchyRepository.GetAccountsForSyncAsync(
            request.WorkspaceId,
            request.ConnectedAdAccountId,
            request.Platform,
            cancellationToken);

        if (accounts.Count == 0)
        {
            if (request.ConnectedAdAccountId.HasValue)
            {
                return Result<SyncCampaignMetricsSummaryDto>.Failure(
                    Error.NotFound("SyncCampaignMetrics.AccountNotFound", "A conta de anúncios solicitada não foi localizada."));
            }

            return Result<SyncCampaignMetricsSummaryDto>.Success(
                new SyncCampaignMetricsSummaryDto(
                    0, 0, 0m, 0L, 0L, 0m, 0m, DateTime.UtcNow, request.Granularity.ToString(), Array.Empty<string>()));
        }

        var totalAccounts = 0;
        var totalRecords = 0;
        var totalSpend = 0m;
        var totalImpressions = 0L;
        var totalClicks = 0L;
        var totalConversions = 0m;
        var totalConversionValue = 0m;
        var syncedPlatforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var syncTimeUtc = DateTime.UtcNow;

        foreach (var account in accounts)
        {
            string? decryptedToken = null;

            if (!account.IsDemo)
            {
                var vault = await _tokenVaultRepository.GetByWorkspaceAndPlatformAsync(
                    account.WorkspaceId,
                    account.Platform,
                    cancellationToken);

                if (vault is null || string.IsNullOrWhiteSpace(vault.EncryptedAccessToken))
                {
                    if (request.ConnectedAdAccountId.HasValue)
                    {
                        return Result<SyncCampaignMetricsSummaryDto>.Failure(
                            Error.Unauthorized("SyncCampaignMetrics.CredentialsNotFound",
                                $"Credenciais OAuth2 para {account.Platform} não encontradas no Token Vault."));
                    }

                    continue;
                }

                try
                {
                    decryptedToken = _encryptionService.Decrypt(vault.EncryptedAccessToken);
                }
                catch (Exception ex)
                {
                    if (request.ConnectedAdAccountId.HasValue)
                    {
                        return Result<SyncCampaignMetricsSummaryDto>.Failure(
                            Error.Failure("SyncCampaignMetrics.DecryptionError", $"Falha ao descriptografar token: {ex.Message}"));
                    }

                    continue;
                }
            }

            var fetchResult = await _syncDispatcher.DispatchAsync(
                account,
                decryptedToken,
                request.StartDateUtc,
                request.EndDateUtc,
                request.Granularity,
                cancellationToken);

            if (fetchResult.IsFailure)
            {
                if (request.ConnectedAdAccountId.HasValue)
                {
                    return Result<SyncCampaignMetricsSummaryDto>.Failure(fetchResult.Error);
                }

                continue;
            }

            var fetchedItems = fetchResult.Value;
            if (fetchedItems.Count == 0)
            {
                totalAccounts++;
                syncedPlatforms.Add(account.Platform);
                continue;
            }

            // Mapeia campanhas existentes para resolução dos IDs locais
            var existingCampaigns = await _hierarchyRepository.GetCampaignsByWorkspaceAsync(
                request.WorkspaceId,
                account.Id,
                account.Platform,
                null,
                cancellationToken);

            var campaignMap = existingCampaigns.ToDictionary(
                c => c.ExternalCampaignId,
                c => c.Id,
                StringComparer.OrdinalIgnoreCase);

            var metricEntities = new List<CampaignMetric>();
            var accountSpend = 0m;

            foreach (var item in fetchedItems)
            {
                // Se a campanha já existe, vincula seu CampaignId. Se não existir, resolve com Id gerado
                if (!campaignMap.TryGetValue(item.ExternalCampaignId, out var localCampaignId))
                {
                    localCampaignId = Guid.NewGuid();
                }

                var entityResult = CampaignMetric.Create(
                    id: Guid.NewGuid(),
                    workspaceId: request.WorkspaceId,
                    connectedAdAccountId: account.Id,
                    campaignId: localCampaignId,
                    adSetId: null,
                    adId: null,
                    platform: account.Platform,
                    externalCampaignId: item.ExternalCampaignId,
                    externalAdSetId: item.ExternalAdSetId,
                    externalAdId: item.ExternalAdId,
                    date: item.Date,
                    hour: item.Hour,
                    granularity: request.Granularity,
                    spend: item.Spend,
                    currency: item.Currency,
                    impressions: item.Impressions,
                    clicks: item.Clicks,
                    conversions: item.Conversions,
                    conversionValue: item.ConversionValue,
                    syncedAtUtc: syncTimeUtc);

                if (entityResult.IsSuccess)
                {
                    metricEntities.Add(entityResult.Value);
                    accountSpend += item.Spend;
                    totalSpend += item.Spend;
                    totalImpressions += item.Impressions;
                    totalClicks += item.Clicks;
                    totalConversions += item.Conversions;
                    totalConversionValue += item.ConversionValue;
                }
            }

            if (metricEntities.Count > 0)
            {
                var upsertResult = await _metricsRepository.UpsertMetricsBatchAsync(
                    request.WorkspaceId,
                    metricEntities,
                    cancellationToken);

                if (upsertResult.IsFailure)
                {
                    if (request.ConnectedAdAccountId.HasValue)
                    {
                        return Result<SyncCampaignMetricsSummaryDto>.Failure(upsertResult.Error);
                    }

                    continue;
                }

                totalRecords += upsertResult.Value;
            }

            totalAccounts++;
            syncedPlatforms.Add(account.Platform);

            // Emite evento de domínio in-memory para integração reativa entre módulos
            var tenantId = _tenantContextAccessor.TenantContext?.TenantId ?? Guid.Empty;
            var domainEvent = new CampaignMetricsSyncedEvent(
                tenantId,
                request.WorkspaceId,
                account.Id,
                account.Platform,
                metricEntities.Count,
                request.Granularity,
                accountSpend,
                syncTimeUtc);

            await _publisher.Publish(domainEvent, cancellationToken);
        }

        var summary = new SyncCampaignMetricsSummaryDto(
            TotalAccountsProcessed: totalAccounts,
            TotalRecordsIngested: totalRecords,
            TotalSpend: totalSpend,
            TotalImpressions: totalImpressions,
            TotalClicks: totalClicks,
            TotalConversions: totalConversions,
            TotalConversionValue: totalConversionValue,
            SyncedAtUtc: syncTimeUtc,
            Granularity: request.Granularity.ToString(),
            SyncedPlatforms: syncedPlatforms.ToList());

        return Result<SyncCampaignMetricsSummaryDto>.Success(summary);
    }
}
