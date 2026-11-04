using Analytics.Application.Creatives.Notifications;
using Analytics.Domain;
using Analytics.Domain.Creatives;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SocialPublishing.Contracts.Notifications;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes do handler que vincula tração orgânica ao Creative Hub (Subfase 6.2).
/// </summary>
public sealed class LinkOrganicAssetWhenTractionDetectedTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
    private static readonly DateTime DetectedAtUtc = new(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Segunda notificação com a mesma fingerprint atualiza o mesmo ativo sem duplicar.
    /// </summary>
    [Fact]
    public async Task Handle_SegundaNotificacaoMesmaFingerprint_AtualizaSemDuplicar()
    {
        var repository = new InMemoryCreativeHubAssetRepository();
        var handler = new LinkOrganicAssetWhenTractionDetected(
            repository,
            NullLogger<LinkOrganicAssetWhenTractionDetected>.Instance);

        var postA = Guid.NewGuid();
        var postB = Guid.NewGuid();
        const string fingerprint = "abc123fingerprint";

        await handler.Handle(CreateNotification(postA, fingerprint, "https://example.com/a"), CancellationToken.None);
        await handler.Handle(
            CreateNotification(postB, fingerprint, "https://example.com/b"),
            CancellationToken.None);

        repository.StoredCount.Should().Be(1);
        var stored = repository.Single();
        stored.OrganicPostId.Should().Be(postB);
        stored.Permalink.Should().Be("https://example.com/b");
        stored.Impressions.Should().Be(5000);
    }

    /// <summary>
    /// Fingerprint ausente não persiste ativo e não lança exceção.
    /// </summary>
    [Fact]
    public async Task Handle_FingerprintAusente_NaoPersisteAtivo()
    {
        var repository = Substitute.For<ICreativeHubAssetRepository>();
        var handler = new LinkOrganicAssetWhenTractionDetected(
            repository,
            NullLogger<LinkOrganicAssetWhenTractionDetected>.Instance);

        await handler.Handle(CreateNotification(Guid.NewGuid(), mediaFingerprint: null, permalink: null), CancellationToken.None);

        await repository.DidNotReceive().UpsertByFingerprintAsync(
            Arg.Any<CreativeHubAssetUpsertRequest>(),
            Arg.Any<CancellationToken>());
    }

    private static OrganicTractionDetectedNotification CreateNotification(
        Guid organicPostId,
        string? mediaFingerprint,
        string? permalink) =>
        new(
            TenantId: Guid.NewGuid(),
            WorkspaceId: WorkspaceId,
            OrganicPostId: organicPostId,
            Network: "Instagram",
            MediaFingerprint: mediaFingerprint,
            Permalink: permalink,
            Impressions: 5000,
            EngagementRate: 0.05m,
            BaselineMedianEngagement: 0.02m,
            EngagementMultiple: 2.5m,
            DetectedAtUtc: DetectedAtUtc);

    private sealed class InMemoryCreativeHubAssetRepository : ICreativeHubAssetRepository
    {
        private readonly Dictionary<(Guid WorkspaceId, string Fingerprint), CreativeHubAsset> _store = new();

        public int StoredCount => _store.Count;

        public CreativeHubAsset Single() => _store.Values.Single();

        public Task<Result<Guid>> UpsertByFingerprintAsync(
            CreativeHubAssetUpsertRequest request,
            CancellationToken cancellationToken = default)
        {
            var key = (request.WorkspaceId, request.MediaFingerprint);
            if (_store.TryGetValue(key, out var existing))
            {
                existing.UpdateOrganicTraction(
                    request.OrganicPostId,
                    request.Network,
                    request.Permalink,
                    request.Impressions,
                    request.EngagementRate,
                    request.BaselineMedianEngagement,
                    request.EngagementMultiple,
                    request.DetectedAtUtc);
                return Task.FromResult(Result<Guid>.Success(existing.Id));
            }

            var asset = new CreativeHubAsset(
                Guid.NewGuid(),
                request.WorkspaceId,
                request.MediaFingerprint,
                request.OrganicPostId,
                request.Network,
                request.Permalink,
                request.Impressions,
                request.EngagementRate,
                request.BaselineMedianEngagement,
                request.EngagementMultiple,
                request.DetectedAtUtc);

            _store[key] = asset;
            return Task.FromResult(Result<Guid>.Success(asset.Id));
        }
    }
}
