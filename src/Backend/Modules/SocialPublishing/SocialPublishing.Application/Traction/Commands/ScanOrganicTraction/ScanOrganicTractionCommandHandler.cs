using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Insights;
using SocialPublishing.Application.Traction;
using SocialPublishing.Contracts.Notifications;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Traction;

namespace SocialPublishing.Application.Traction.Commands.ScanOrganicTraction;

/// <summary>
/// Varre posts publicados, detecta tração anômala e notifica outros módulos após commit bem-sucedido.
/// </summary>
public sealed class ScanOrganicTractionCommandHandler
    : ICommandHandler<ScanOrganicTractionCommand, ScanOrganicTractionSummary>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IOrganicPostRepository _postRepository;
    private readonly IOrganicConnectionRepository _connectionRepository;
    private readonly IPaidBoostSuggestionRepository _suggestionRepository;
    private readonly IOrganicInsightsReader _insightsReader;
    private readonly IOrganicTokenProtector _tokenProtector;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ScanOrganicTractionCommandHandler"/>.
    /// </summary>
    public ScanOrganicTractionCommandHandler(
        ITenantContextAccessor tenantContextAccessor,
        IOrganicPostRepository postRepository,
        IOrganicConnectionRepository connectionRepository,
        IPaidBoostSuggestionRepository suggestionRepository,
        IOrganicInsightsReader insightsReader,
        IOrganicTokenProtector tokenProtector,
        ISocialPublishingUnitOfWork unitOfWork,
        IPublisher publisher,
        TimeProvider timeProvider)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _suggestionRepository = suggestionRepository ?? throw new ArgumentNullException(nameof(suggestionRepository));
        _insightsReader = insightsReader ?? throw new ArgumentNullException(nameof(insightsReader));
        _tokenProtector = tokenProtector ?? throw new ArgumentNullException(nameof(tokenProtector));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result<ScanOrganicTractionSummary>> Handle(
        ScanOrganicTractionCommand request,
        CancellationToken cancellationToken)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (tenantContext?.TenantId is null)
        {
            return Result<ScanOrganicTractionSummary>.Failure(SocialErrors.TenantRequired);
        }

        var asOfUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var windowStart = asOfUtc.AddDays(-OrganicTractionThresholds.BaselineWindowDays);
        var candidates = await _postRepository.ListPublishedForTractionAsync(windowStart, cancellationToken);
        if (candidates.Count == 0)
        {
            return Result<ScanOrganicTractionSummary>.Success(new ScanOrganicTractionSummary(0, 0));
        }

        var suggestionsCreated = 0;
        var insightsFailures = 0;
        var connectionCache = new Dictionary<Guid, OrganicConnection>();

        foreach (var workspaceGroup in candidates.GroupBy(c => c.WorkspaceId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var workspaceId = workspaceGroup.Key;
            var openPostIds = await _suggestionRepository.GetOpenOrganicPostIdsAsync(workspaceId, cancellationToken);
            var samples = new List<OrganicPostInsightSample>();

            foreach (var candidate in workspaceGroup)
            {
                if (!connectionCache.TryGetValue(candidate.ConnectionId, out var connection))
                {
                    connection = await _connectionRepository.GetByIdAsync(candidate.ConnectionId, cancellationToken);
                    if (connection is null || connection.Status != OrganicConnectionStatus.Active)
                    {
                        insightsFailures++;
                        continue;
                    }

                    connectionCache[candidate.ConnectionId] = connection;
                }

                var purpose = OrganicConnection.EncryptionPurpose(
                    connection.Platform,
                    connection.WorkspaceId,
                    connection.Id);
                var token = _tokenProtector.Unprotect(connection.EncryptedAccessToken, purpose);
                if (token.IsFailure || string.IsNullOrWhiteSpace(token.Value))
                {
                    insightsFailures++;
                    continue;
                }

                var insightResult = await _insightsReader.ReadAsync(
                    new OrganicInsightsReadRequest(
                        token.Value,
                        connection.ExternalAccountId,
                        candidate.PublishedExternalId,
                        candidate.Platform),
                    cancellationToken);

                if (insightResult.IsFailure)
                {
                    insightsFailures++;
                    continue;
                }

                var snapshot = insightResult.Value;
                samples.Add(
                    new OrganicPostInsightSample(
                        workspaceId,
                        candidate.OrganicPostId,
                        candidate.Platform,
                        candidate.PublishedAtUtc,
                        snapshot.Impressions,
                        snapshot.Likes,
                        snapshot.Comments,
                        snapshot.Shares,
                        snapshot.Saves,
                        openPostIds.Contains(candidate.OrganicPostId)));
            }

            if (samples.Count == 0)
            {
                continue;
            }

            var detectResult = OrganicTractionDetector.Detect(workspaceId, samples, asOfUtc);
            if (detectResult.IsFailure)
            {
                continue;
            }

            foreach (var hit in detectResult.Value.Hits)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var candidate = workspaceGroup.First(c => c.OrganicPostId == hit.OrganicPostId);
                var suggestionResult = PaidBoostSuggestion.Open(
                    workspaceId,
                    hit.OrganicPostId,
                    hit.Platform,
                    hit.Impressions,
                    hit.EngagementRate,
                    hit.BaselineMedianEngagement,
                    hit.EngagementMultiple,
                    asOfUtc);

                if (suggestionResult.IsFailure)
                {
                    continue;
                }

                await _suggestionRepository.AddAsync(suggestionResult.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                var fingerprint = OrganicMediaFingerprint.FromObjectKey(candidate.PrimaryMediaObjectKey);
                await _publisher.Publish(
                    new OrganicTractionDetectedNotification(
                        tenantContext.TenantId.Value,
                        workspaceId,
                        hit.OrganicPostId,
                        hit.Platform.ToString(),
                        fingerprint,
                        Permalink: null,
                        hit.Impressions,
                        hit.EngagementRate,
                        hit.BaselineMedianEngagement,
                        hit.EngagementMultiple,
                        asOfUtc),
                    cancellationToken);

                openPostIds = await _suggestionRepository.GetOpenOrganicPostIdsAsync(workspaceId, cancellationToken);
                suggestionsCreated++;
            }
        }

        return Result<ScanOrganicTractionSummary>.Success(
            new ScanOrganicTractionSummary(suggestionsCreated, insightsFailures));
    }
}
