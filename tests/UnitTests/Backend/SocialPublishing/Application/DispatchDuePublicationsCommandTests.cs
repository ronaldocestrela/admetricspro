using System.Reflection;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Publishing;
using SocialPublishing.Application.Publishing.Commands.DispatchDuePublications;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Domain.Publications;
using SocialPublishing.Domain.Publishing;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando de disparo de publicações orgânicas devidas.
/// </summary>
public sealed class DispatchDuePublicationsCommandTests
{
    private static readonly DateTime FixedUtc = new(2026, 7, 1, 14, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Job devido publica com sucesso e persiste id externo.
    /// </summary>
    [Fact]
    public async Task Handle_JobDevido_PublicaComSucesso()
    {
        var host = CreateHost();
        var (post, job) = await SeedScheduledJobAsync(host, FixedUtc.AddMinutes(-1));
        host.Adapter.PublishAsync(Arg.Any<OrganicPublishRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<OrganicPublishReceipt>.Success(new OrganicPublishReceipt("ext-999")));

        var result = await host.Handler.Handle(new DispatchDuePublicationsCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SucceededCount.Should().Be(1);
        post.Status.Should().Be(OrganicPostStatus.Published);
        post.PublishedExternalId.Should().Be("ext-999");
        job.Status.Should().Be(PublicationJobStatus.Succeeded);
        await host.Adapter.Received(1).PublishAsync(Arg.Any<OrganicPublishRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rate limit reagenda job e devolve post para Scheduled.
    /// </summary>
    [Fact]
    public async Task Handle_RateLimited_ReagendaRetentativa()
    {
        var host = CreateHost();
        var (_, job) = await SeedScheduledJobAsync(host, FixedUtc.AddMinutes(-1));
        job.GetType(); // tracked reference
        host.Adapter.PublishAsync(Arg.Any<OrganicPublishRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<OrganicPublishReceipt>.Failure(
                new PublishRateLimitedError(SocialErrors.PublishRateLimited.Description, TimeSpan.FromSeconds(30))));

        var result = await host.Handler.Handle(new DispatchDuePublicationsCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RetryScheduledCount.Should().Be(1);
        var post = host.PostRepository.AllPosts.Single();
        post.Status.Should().Be(OrganicPostStatus.Scheduled);
        job.Status.Should().Be(PublicationJobStatus.RetryScheduled);
        job.NextAttemptAtUtc.Should().NotBeNull();
        job.NextAttemptAtUtc!.Value.Should().BeOnOrAfter(FixedUtc.AddSeconds(30));
        job.AttemptCount.Should().Be(1);
    }

    /// <summary>
    /// Teto de tentativas evita nova chamada ao adaptador.
    /// </summary>
    [Fact]
    public async Task Handle_TetoDeTentativas_DeadLetterSemAdaptador()
    {
        var host = CreateHost(maxAttempts: 3);
        var (post, job) = await SeedScheduledJobAsync(host, FixedUtc.AddMinutes(-1));
        SetAttemptCount(job, 3);

        var result = await host.Handler.Handle(new DispatchDuePublicationsCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeadLetterCount.Should().Be(1);
        post.Status.Should().Be(OrganicPostStatus.Failed);
        job.Status.Should().Be(PublicationJobStatus.DeadLetter);
        await host.Adapter.DidNotReceive().PublishAsync(Arg.Any<OrganicPublishRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Cancelamento do host não marca falha definitiva.
    /// </summary>
    [Fact]
    public async Task Handle_Cancelamento_NaoMarcaDeadLetter()
    {
        var host = CreateHost();
        await SeedScheduledJobAsync(host, FixedUtc.AddMinutes(-1));
        using var cts = new CancellationTokenSource();
        host.Adapter.PublishAsync(Arg.Any<OrganicPublishRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                cts.Cancel();
                return Task.FromException<Result<OrganicPublishReceipt>>(new OperationCanceledException(cts.Token));
            });

        var act = () => host.Handler.Handle(new DispatchDuePublicationsCommand(), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        host.PublicationJobRepository.AllJobs.Single().Status.Should().Be(PublicationJobStatus.InProgress);
        host.PostRepository.AllPosts.Single().Status.Should().Be(OrganicPostStatus.Publishing);
    }

    private static async Task<(OrganicPost Post, PublicationJob Job)> SeedScheduledJobAsync(
        DispatchTestHost host,
        DateTime scheduledAtUtc)
    {
        var connectionId = Guid.NewGuid();
        var connection = OrganicConnection.Create(
            host.WorkspaceId,
            OrganicPlatform.Instagram,
            "ig-user-1",
            "Conta",
            "scopes",
            "cipher-token",
            id: connectionId).Value;
        host.ConnectionRepository.AllConnections.Add(connection);

        var post = OrganicPost.Create(
            host.WorkspaceId,
            connectionId,
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            global::SocialPublishing.Domain.Brand.ContentPillar.Authority,
            "Legenda",
            scheduledAtUtc,
            "UTC",
            [(1080, 1080, 0)]).Value;
        SetMediaObjectKey(post, "tenant/ws/asset.jpg");
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Scheduled);
        host.PostRepository.AllPosts.Add(post);

        var job = PublicationJob.Open(post.Id, post.WorkspaceId, post.ScheduledAtUtc, FixedUtc.AddDays(-1)).Value;
        await host.PublicationJobRepository.AddAsync(job, CancellationToken.None);
        return (post, job);
    }

    private static void SetMediaObjectKey(OrganicPost post, string objectKey)
    {
        var asset = post.Media.Single();
        typeof(OrganicMediaAsset).GetProperty(nameof(OrganicMediaAsset.ObjectKey))!
            .SetValue(asset, objectKey);
    }

    private static void SetAttemptCount(PublicationJob job, int count)
    {
        typeof(PublicationJob).GetProperty(nameof(PublicationJob.AttemptCount))!
            .SetValue(job, count);
    }

    private static DispatchTestHost CreateHost(int maxAttempts = 3) =>
        new(maxAttempts);

    private sealed class DispatchTestHost
    {
        public DispatchTestHost(int maxAttempts)
        {
            WorkspaceId = Guid.NewGuid();
            PostRepository = new InMemoryOrganicPostRepository();
            PublicationJobRepository = new InMemoryPublicationJobRepository();
            ConnectionRepository = new InMemoryOrganicConnectionRepository();
            UnitOfWork = Substitute.For<ISocialPublishingUnitOfWork>();
            UnitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

            Clock = Substitute.For<TimeProvider>();
            Clock.GetUtcNow().Returns(new DateTimeOffset(FixedUtc));

            TokenProtector = Substitute.For<IOrganicTokenProtector>();
            TokenProtector.Unprotect(Arg.Any<string>(), Arg.Any<string>())
                .Returns(Result<string>.Success("plain-token"));

            MediaUrlResolver = Substitute.For<IOrganicMediaPublicUrlResolver>();
            MediaUrlResolver.ResolvePublicUrl(Arg.Any<string>())
                .Returns(call => Result<string>.Success($"https://cdn.test/{call.Arg<string>()}"));

            Jitter = Substitute.For<IOrganicPublishJitterProvider>();
            Jitter.GetJitterMilliseconds().Returns(0);

            Adapter = Substitute.For<IOrganicPlatformAdapter>();
            Adapter.Platform.Returns(OrganicPlatform.Instagram);

            var policy = new OrganicPublishRateLimitPolicy(500, 10_000, maxAttempts);
            Handler = new DispatchDuePublicationsCommandHandler(
                PublicationJobRepository,
                PostRepository,
                ConnectionRepository,
                TokenProtector,
                MediaUrlResolver,
                [Adapter],
                policy,
                Jitter,
                UnitOfWork,
                Clock,
                Options.Create(new OrganicPublishOptions { MaxAttempts = maxAttempts }));
        }

        public Guid WorkspaceId { get; }
        public InMemoryOrganicPostRepository PostRepository { get; }
        public InMemoryPublicationJobRepository PublicationJobRepository { get; }
        public InMemoryOrganicConnectionRepository ConnectionRepository { get; }
        public ISocialPublishingUnitOfWork UnitOfWork { get; }
        public IOrganicTokenProtector TokenProtector { get; }
        public IOrganicMediaPublicUrlResolver MediaUrlResolver { get; }
        public IOrganicPublishJitterProvider Jitter { get; }
        public TimeProvider Clock { get; }
        public IOrganicPlatformAdapter Adapter { get; }
        public DispatchDuePublicationsCommandHandler Handler { get; }
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
            Task.FromResult(AllConnections.FirstOrDefault(c =>
                c.WorkspaceId == workspaceId &&
                c.Platform == platform &&
                c.ExternalAccountId == externalAccountId));

        public Task<IReadOnlyList<OrganicConnection>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicConnection>>(AllConnections.Where(c => c.WorkspaceId == workspaceId).ToList());

        public Task AddAsync(OrganicConnection connection, CancellationToken cancellationToken = default)
        {
            AllConnections.Add(connection);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganicConnection connection, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> CountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(AllConnections.Count(c => c.WorkspaceId == workspaceId));
    }
}
