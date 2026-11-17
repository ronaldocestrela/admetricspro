using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using MediatR;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Insights;
using SocialPublishing.Application.Traction;
using SocialPublishing.Application.Traction.Commands.ScanOrganicTraction;
using SocialPublishing.Contracts.Notifications;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Traction;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando de scan de tração orgânica (Subfase 6.2).
/// </summary>
public sealed class ScanOrganicTractionCommandTests
{
    private static readonly DateTime FixedUtc = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Hit persiste sugestão Open e publica notificação somente após commit.
    /// </summary>
    [Fact]
    public async Task Handle_ComHit_PersisteSugestaoEPublicaNotificacaoAposCommit()
    {
        var host = CreateHost();
        var hitPostId = Guid.NewGuid();
        SeedTractionSeries(host, hitPostId);

        var result = await host.Handler.Handle(new ScanOrganicTractionCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SuggestionsCreated.Should().Be(1);
        host.SuggestionRepository.AllSuggestions.Should().ContainSingle(s => s.Status == PaidBoostSuggestionStatus.Open);
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await host.Publisher.Received(1).Publish(
            Arg.Is<OrganicTractionDetectedNotification>(n =>
                n.TenantId == TenantId &&
                n.OrganicPostId == hitPostId &&
                n.Network == OrganicPlatform.Instagram.ToString() &&
                n.MediaFingerprint == OrganicMediaFingerprint.FromObjectKey("media/hit.jpg")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Falha de commit não publica notificação.
    /// </summary>
    [Fact]
    public async Task Handle_CommitFalha_NaoPublicaNotificacao()
    {
        var host = CreateHost();
        SeedTractionSeries(host, Guid.NewGuid());
        host.UnitOfWork
            .CommitAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("commit failed"));

        var act = () => host.Handler.Handle(new ScanOrganicTractionCommand(), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
        await host.Publisher.DidNotReceive().Publish(
            Arg.Any<OrganicTractionDetectedNotification>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Post com sugestão aberta não gera nova sugestão nem notificação.
    /// </summary>
    [Fact]
    public async Task Handle_ComSugestaoAberta_NaoCriaNovaSugestao()
    {
        var host = CreateHost();
        var hitPostId = Guid.NewGuid();
        host.SuggestionRepository.OpenPostIds.Add(hitPostId);
        SeedTractionSeries(host, hitPostId);

        var result = await host.Handler.Handle(new ScanOrganicTractionCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SuggestionsCreated.Should().Be(0);
        host.SuggestionRepository.AllSuggestions.Should().BeEmpty();
        await host.Publisher.DidNotReceive().Publish(
            Arg.Any<OrganicTractionDetectedNotification>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Falha de insights em um post não interrompe o scan.
    /// </summary>
    [Fact]
    public async Task Handle_InsightsFalham_IgnoraPostEContinua()
    {
        var host = CreateHost();
        var hitPostId = Guid.NewGuid();
        SeedTractionSeries(host, hitPostId);
        host.InsightsReader.ReadAsync(
                Arg.Is<OrganicInsightsReadRequest>(r => r.PublishedExternalId == "ext-fail"),
                Arg.Any<CancellationToken>())
            .Returns(Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed));

        var result = await host.Handler.Handle(new ScanOrganicTractionCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InsightsFailures.Should().BeGreaterThanOrEqualTo(1);
    }

    /// <summary>
    /// Sem tenant context o comando falha antes de acessar repositórios.
    /// </summary>
    [Fact]
    public async Task Handle_SemTenantContext_RetornaTenantRequired()
    {
        var host = CreateHost();
        ((TenantContextAccessor)host.TenantAccessor).TenantContext = null!;

        var result = await host.Handler.Handle(new ScanOrganicTractionCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.TenantRequired.Code);
        await host.PostRepository.DidNotReceive().ListPublishedForTractionAsync(
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    private static void SeedTractionSeries(ScanTestHost host, Guid hitPostId)
    {
        var connectionId = Guid.NewGuid();
        var connection = OrganicConnection.Create(
            host.WorkspaceId,
            OrganicPlatform.Instagram,
            "ig-user",
            "Conta",
            "scopes",
            "cipher",
            id: connectionId).Value;
        host.ConnectionRepository.AllConnections.Add(connection);

        var baseline1 = Guid.NewGuid();
        var baseline2 = Guid.NewGuid();
        host.InMemoryPosts.TractionCandidates.AddRange(
        [
            CreateCandidate(baseline1, host.WorkspaceId, connectionId, "ext-1", FixedUtc.AddDays(-1)),
            CreateCandidate(baseline2, host.WorkspaceId, connectionId, "ext-2", FixedUtc.AddDays(-2)),
            CreateCandidate(hitPostId, host.WorkspaceId, connectionId, "ext-hit", FixedUtc.AddDays(-3), "media/hit.jpg"),
            CreateCandidate(Guid.NewGuid(), host.WorkspaceId, connectionId, "ext-fail", FixedUtc.AddDays(-4)),
        ]);

        host.InsightsReader.ReadAsync(Arg.Any<OrganicInsightsReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<OrganicInsightsReadRequest>();
                return request.PublishedExternalId switch
                {
                    "ext-1" => Result<OrganicPostInsightSnapshot>.Success(new(1000, 20, 0, 0, 0)),
                    "ext-2" => Result<OrganicPostInsightSnapshot>.Success(new(1000, 20, 0, 0, 0)),
                    "ext-hit" => Result<OrganicPostInsightSnapshot>.Success(new(1000, 60, 0, 0, 0)),
                    "ext-fail" => Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed),
                    _ => Result<OrganicPostInsightSnapshot>.Failure(SocialErrors.InsightsReadFailed),
                };
            });
    }

    private static OrganicPostTractionCandidate CreateCandidate(
        Guid postId,
        Guid workspaceId,
        Guid connectionId,
        string externalId,
        DateTime publishedAtUtc,
        string? objectKey = "media/default.jpg") =>
        new(postId, workspaceId, connectionId, OrganicPlatform.Instagram, publishedAtUtc, externalId, objectKey);

    private static ScanTestHost CreateHost()
    {
        var tenantAccessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(TenantId, "acme", TenantResolutionSource.None),
        };

        var postRepository = Substitute.For<IOrganicPostRepository>();
        var inMemoryPosts = new InMemoryOrganicPostRepository();
        postRepository
            .ListPublishedForTractionAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => inMemoryPosts.ListPublishedForTractionAsync(call.Arg<DateTime>(), call.Arg<CancellationToken>()));

        var host = new ScanTestHost(tenantAccessor, postRepository, inMemoryPosts);
        return host;
    }

    private sealed class ScanTestHost
    {
        public ScanTestHost(
            ITenantContextAccessor tenantAccessor,
            IOrganicPostRepository postRepository,
            InMemoryOrganicPostRepository inMemoryPosts)
        {
            TenantAccessor = tenantAccessor;
            PostRepository = postRepository;
            InMemoryPosts = inMemoryPosts;
            ConnectionRepository = new InMemoryOrganicConnectionRepository();
            SuggestionRepository = new InMemoryPaidBoostSuggestionRepository();
            InsightsReader = Substitute.For<IOrganicInsightsReader>();
            TokenProtector = Substitute.For<IOrganicTokenProtector>();
            TokenProtector.Unprotect(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Result<string>.Success("token"));
            UnitOfWork = Substitute.For<ISocialPublishingUnitOfWork>();
            UnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);
            Publisher = Substitute.For<IPublisher>();
            Clock = Substitute.For<TimeProvider>();
            Clock.GetUtcNow().Returns(new DateTimeOffset(FixedUtc));
            WorkspaceId = Guid.NewGuid();

            Handler = new ScanOrganicTractionCommandHandler(
                TenantAccessor,
                PostRepository,
                ConnectionRepository,
                SuggestionRepository,
                InsightsReader,
                TokenProtector,
                UnitOfWork,
                Publisher,
                Clock);
        }

        public Guid WorkspaceId { get; }
        public ITenantContextAccessor TenantAccessor { get; }
        public IOrganicPostRepository PostRepository { get; }
        public InMemoryOrganicPostRepository InMemoryPosts { get; }
        public InMemoryOrganicConnectionRepository ConnectionRepository { get; }
        public InMemoryPaidBoostSuggestionRepository SuggestionRepository { get; }
        public IOrganicInsightsReader InsightsReader { get; }
        public IOrganicTokenProtector TokenProtector { get; }
        public ISocialPublishingUnitOfWork UnitOfWork { get; }
        public IPublisher Publisher { get; }
        public TimeProvider Clock { get; }
        public ScanOrganicTractionCommandHandler Handler { get; }
    }

    private sealed class InMemoryPaidBoostSuggestionRepository : IPaidBoostSuggestionRepository
    {
        public List<PaidBoostSuggestion> AllSuggestions { get; } = new();
        public HashSet<Guid> OpenPostIds { get; } = new();

        public Task AddAsync(PaidBoostSuggestion suggestion, CancellationToken cancellationToken = default)
        {
            AllSuggestions.Add(suggestion);
            OpenPostIds.Add(suggestion.OrganicPostId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlySet<Guid>> GetOpenOrganicPostIdsAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(OpenPostIds);

        public Task<PaidBoostSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(AllSuggestions.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<PaidBoostSuggestion>> ListByWorkspaceAsync(
            Guid workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PaidBoostSuggestion>>(
                AllSuggestions.Where(s => s.WorkspaceId == workspaceId).ToList());

        public Task UpdateAsync(PaidBoostSuggestion suggestion, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryOrganicConnectionRepository : IOrganicConnectionRepository
    {
        public List<OrganicConnection> AllConnections { get; } = new();

        public Task<OrganicConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(AllConnections.FirstOrDefault(c => c.Id == id));

        public Task<OrganicConnection?> GetByWorkspacePlatformAndExternalAccountAsync(
            Guid workspaceId,
            OrganicPlatform platform,
            string externalAccountId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganicConnection?>(null);

        public Task<IReadOnlyList<OrganicConnection>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicConnection>>(AllConnections);

        public Task AddAsync(OrganicConnection connection, CancellationToken cancellationToken = default)
        {
            AllConnections.Add(connection);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganicConnection connection, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> CountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
