using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Campaigns.Events;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Campaigns.Sync;
using Integrations.Domain.OAuth;
using MediatR;

namespace Integrations.Application.Campaigns.Commands.SyncCampaignHierarchy;

/// <summary>
/// Manipulador do comando <see cref="SyncCampaignHierarchyCommand"/>.
/// Executa a sincronização paginada das contas conectadas, persiste a hierarquia no TenantDbContext,
/// consolida as transações e emite o evento in-memory <see cref="CampaignHierarchySyncedEvent"/>.
/// </summary>
public sealed class SyncCampaignHierarchyCommandHandler : ICommandHandler<SyncCampaignHierarchyCommand, SyncCampaignHierarchySummaryDto>
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository;
    private readonly IOAuthTokenVaultRepository _tokenVaultRepository;
    private readonly IOAuthEncryptionService _encryptionService;
    private readonly ICampaignHierarchySyncDispatcher _syncDispatcher;
    private readonly IIntegrationsUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SyncCampaignHierarchyCommandHandler"/>.
    /// </summary>
    public SyncCampaignHierarchyCommandHandler(
        ICampaignHierarchyRepository hierarchyRepository,
        IOAuthTokenVaultRepository tokenVaultRepository,
        IOAuthEncryptionService encryptionService,
        ICampaignHierarchySyncDispatcher syncDispatcher,
        IIntegrationsUnitOfWork unitOfWork,
        IPublisher publisher,
        ITenantContextAccessor tenantContextAccessor)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _tokenVaultRepository = tokenVaultRepository ?? throw new ArgumentNullException(nameof(tokenVaultRepository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _syncDispatcher = syncDispatcher ?? throw new ArgumentNullException(nameof(syncDispatcher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<SyncCampaignHierarchySummaryDto>> Handle(
        SyncCampaignHierarchyCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<SyncCampaignHierarchySummaryDto>.Failure(
                Error.Validation("SyncCampaignHierarchy.EmptyWorkspaceId", "O workspace é obrigatório para a sincronização."));
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
                return Result<SyncCampaignHierarchySummaryDto>.Failure(
                    Error.NotFound("SyncCampaignHierarchy.AccountNotFound", "A conta de anúncios solicitada não foi localizada."));
            }

            return Result<SyncCampaignHierarchySummaryDto>.Success(
                new SyncCampaignHierarchySummaryDto(0, 0, 0, 0, DateTime.UtcNow, Array.Empty<string>()));
        }

        var totalAccounts = 0;
        var totalCampaigns = 0;
        var totalAdSets = 0;
        var totalAds = 0;
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
                        return Result<SyncCampaignHierarchySummaryDto>.Failure(
                            Error.Unauthorized("SyncCampaignHierarchy.CredentialsNotFound",
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
                        return Result<SyncCampaignHierarchySummaryDto>.Failure(
                            Error.Failure("SyncCampaignHierarchy.DecryptionFailed", $"Falha ao descriptografar token: {ex.Message}"));
                    }

                    continue;
                }
            }

            // Busca a hierarquia via despachante
            var fetchResult = await _syncDispatcher.DispatchAsync(account, decryptedToken, cancellationToken);
            if (fetchResult.IsFailure)
            {
                if (request.ConnectedAdAccountId.HasValue)
                {
                    return Result<SyncCampaignHierarchySummaryDto>.Failure(fetchResult.Error);
                }

                continue;
            }

            var hierarchy = fetchResult.Value;

            // Persiste no banco dedicado do inquilino
            var upsertResult = await _hierarchyRepository.UpsertHierarchyBatchAsync(
                account.WorkspaceId,
                account.Id,
                account.Platform,
                hierarchy,
                syncTimeUtc,
                cancellationToken);

            if (upsertResult.IsFailure)
            {
                if (request.ConnectedAdAccountId.HasValue)
                {
                    return Result<SyncCampaignHierarchySummaryDto>.Failure(upsertResult.Error);
                }

                continue;
            }

            // Consolida a transação
            await _unitOfWork.CommitAsync(cancellationToken);

            // Emite o evento in-memory CampaignHierarchySyncedEvent
            var tenantId = _tenantContextAccessor.TenantContext?.TenantId ?? Guid.Empty;
            var domainEvent = new CampaignHierarchySyncedEvent(
                tenantId,
                account.WorkspaceId,
                account.Id,
                account.Platform,
                hierarchy.Campaigns.Count,
                hierarchy.AdSets.Count,
                hierarchy.Ads.Count,
                syncTimeUtc);

            await _publisher.Publish(
                new DomainEventNotification<CampaignHierarchySyncedEvent>(domainEvent),
                cancellationToken);

            totalAccounts++;
            totalCampaigns += hierarchy.Campaigns.Count;
            totalAdSets += hierarchy.AdSets.Count;
            totalAds += hierarchy.Ads.Count;
            syncedPlatforms.Add(account.Platform);
        }

        return Result<SyncCampaignHierarchySummaryDto>.Success(
            new SyncCampaignHierarchySummaryDto(
                totalAccounts,
                totalCampaigns,
                totalAdSets,
                totalAds,
                syncTimeUtc,
                syncedPlatforms.ToList()));
    }
}
