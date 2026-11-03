using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Abstractions;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Connections.Events;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Infrastructure.Persistence;
using SocialPublishing.Infrastructure.Persistence.Repositories;

namespace IntegrationTests.SocialPublishing;

/// <summary>
/// Testes de integração dos repositórios e unit of work do SocialPublishing.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class SocialPublishingRepositoryTests(SqlServerFixture fixture)
{
    /// <summary>
    /// Valida round-trip de conexão, post com mídia e brand voice via repositórios.
    /// </summary>
    [Fact]
    public async Task Repositories_Should_PersistAndReloadAggregates()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();

        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var connectionRepository = new OrganicConnectionRepository(accessor);
        var postRepository = new OrganicPostRepository(accessor);
        var brandVoiceRepository = new BrandVoiceRepository(accessor);
        var unitOfWork = new SocialPublishingUnitOfWork(accessor);

        var connection = OrganicConnection.Create(
            workspaceId,
            OrganicPlatform.TikTok,
            "tt-001",
            "TikTok Conta",
            "video.publish",
            "cipher-access",
            "cipher-refresh",
            DateTime.UtcNow.AddDays(14)).Value;

        await connectionRepository.AddAsync(connection, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var post = OrganicPost.Create(
            workspaceId,
            connection.Id,
            OrganicPlatform.TikTok,
            OrganicPostFormat.TikTokVideo,
            ContentPillar.Authority,
            "Legenda curta",
            new DateTime(2026, 6, 2, 10, 0, 0),
            "America/Sao_Paulo",
            new List<(int Width, int Height, int SortOrder)> { (1080, 1920, 0) }).Value;

        await postRepository.AddAsync(post, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var brandVoice = BrandVoiceProfile.Create(
            workspaceId,
            "Profissional",
            "Especialista",
            new[] { "https://example.com/about" },
            new[] { "barato" },
            EmojiPolicy.Sparingly,
            10,
            40,
            20,
            25,
            15).Value;

        await brandVoiceRepository.AddAsync(brandVoice, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var loadedConnection = await connectionRepository.GetByIdAsync(connection.Id, CancellationToken.None);
        loadedConnection.Should().NotBeNull();
        loadedConnection!.EncryptedAccessToken.Should().Be("cipher-access");

        var posts = await postRepository.ListByWorkspaceAsync(workspaceId, CancellationToken.None);
        posts.Should().ContainSingle();
        posts[0].Media.Should().ContainSingle();
        posts[0].Media[0].Width.Should().Be(1080);

        var profiles = await brandVoiceRepository.ListByWorkspaceAsync(workspaceId, CancellationToken.None);
        profiles.Should().ContainSingle();
        profiles[0].MandatoryLinks.Should().Contain("https://example.com/about");
        profiles[0].PillarWeights.Authority.Should().Be(40);
    }

    /// <summary>
    /// Valida cancelamento antecipado nos métodos assíncronos dos repositórios.
    /// </summary>
    [Fact]
    public async Task RepositoryMethods_Should_HonorCancellationToken()
    {
        var tenantId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var connectionRepository = new OrganicConnectionRepository(accessor);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var actGet = () => connectionRepository.GetByIdAsync(Guid.NewGuid(), cts.Token);
        var actList = () => connectionRepository.ListByWorkspaceAsync(Guid.NewGuid(), cts.Token);
        var actAdd = () => connectionRepository.AddAsync(
            OrganicConnection.Create(Guid.NewGuid(), OrganicPlatform.Instagram, "x", "x", "s", "c").Value,
            cts.Token);
        var actUpdate = () => connectionRepository.UpdateAsync(
            OrganicConnection.Create(Guid.NewGuid(), OrganicPlatform.Instagram, "x", "x", "s", "c").Value,
            cts.Token);

        await actGet.Should().ThrowAsync<OperationCanceledException>();
        await actList.Should().ThrowAsync<OperationCanceledException>();
        await actAdd.Should().ThrowAsync<OperationCanceledException>();
        await actUpdate.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Valida publicação de evento de domínio após commit bem-sucedido.
    /// </summary>
    [Fact]
    public async Task CommitAsync_AfterRevoke_Should_PublishDomainEvent()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var connectionRepository = new OrganicConnectionRepository(accessor);
        var publisher = new RecordingPublisher();
        var unitOfWork = new SocialPublishingUnitOfWork(accessor, publisher);

        var connection = OrganicConnection.Create(
            workspaceId,
            OrganicPlatform.FacebookPage,
            "fb-1",
            "Page",
            "pages_manage_posts",
            "cipher").Value;

        await connectionRepository.AddAsync(connection, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        connection.Revoke().IsSuccess.Should().BeTrue();
        await connectionRepository.UpdateAsync(connection, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        publisher.PublishedEvents.Should().ContainSingle(e => e is OrganicConnectionRevoked);
        connection.DomainEvents.Should().BeEmpty();
    }

    private async Task<string> PrepareMigratedDatabaseAsync()
    {
        var databaseName = $"Tenant_SocialRepo_{Guid.NewGuid():N}";
        var connectionString = SocialPublishingTestDatabase.WithDatabase(fixture.ConnectionString, databaseName);
        await SocialPublishingTestDatabase.EnsureDatabaseCreatedAsync(connectionString);

        var migrator = new SocialPublishingSchemaMigrator(new FakeTenantConnectionResolver(new Dictionary<Guid, string>()));
        (await migrator.ApplyAsync(connectionString, CancellationToken.None)).IsSuccess.Should().BeTrue();
        return connectionString;
    }

    private sealed class RecordingPublisher : IPublisher
    {
        /// <summary>
        /// Eventos publicados durante o teste.
        /// </summary>
        public List<IDomainEvent> PublishedEvents { get; } = new();

        /// <inheritdoc />
        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            if (notification is DomainEventNotification<OrganicConnectionRevoked> wrapped)
            {
                PublishedEvents.Add(wrapped.DomainEvent);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Publish((object)notification!, cancellationToken);
    }
}
