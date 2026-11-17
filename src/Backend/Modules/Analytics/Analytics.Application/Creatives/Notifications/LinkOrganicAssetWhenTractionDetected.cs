using Analytics.Domain;
using Analytics.Domain.Creatives;
using MediatR;
using Microsoft.Extensions.Logging;
using SocialPublishing.Contracts.Notifications;

namespace Analytics.Application.Creatives.Notifications;

/// <summary>
/// Registra ou atualiza ativos orgânicos no Creative Hub quando a tração é detectada.
/// </summary>
public sealed class LinkOrganicAssetWhenTractionDetected
    : INotificationHandler<OrganicTractionDetectedNotification>
{
    private readonly ICreativeHubAssetRepository _assetRepository;
    private readonly ILogger<LinkOrganicAssetWhenTractionDetected> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="LinkOrganicAssetWhenTractionDetected"/>.
    /// </summary>
    /// <param name="assetRepository">Repositório de ativos do Creative Hub.</param>
    /// <param name="logger">Logger estruturado.</param>
    public LinkOrganicAssetWhenTractionDetected(
        ICreativeHubAssetRepository assetRepository,
        ILogger<LinkOrganicAssetWhenTractionDetected> logger)
    {
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task Handle(OrganicTractionDetectedNotification notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(notification.MediaFingerprint))
        {
            _logger.LogWarning(
                "Tração orgânica sem fingerprint para post {OrganicPostId} no workspace {WorkspaceId}: {Code}",
                notification.OrganicPostId,
                notification.WorkspaceId,
                AnalyticsErrors.OrganicFingerprintMissing.Code);
            return;
        }

        var upsertResult = await _assetRepository.UpsertByFingerprintAsync(
            new CreativeHubAssetUpsertRequest(
                notification.WorkspaceId,
                notification.MediaFingerprint.Trim(),
                notification.OrganicPostId,
                notification.Network,
                notification.Permalink,
                notification.Impressions,
                notification.EngagementRate,
                notification.BaselineMedianEngagement,
                notification.EngagementMultiple,
                notification.DetectedAtUtc),
            cancellationToken);

        if (upsertResult.IsFailure)
        {
            _logger.LogWarning(
                "Falha ao vincular ativo orgânico {OrganicPostId} ao Creative Hub: {Code}",
                notification.OrganicPostId,
                upsertResult.Error.Code);
        }
    }
}
