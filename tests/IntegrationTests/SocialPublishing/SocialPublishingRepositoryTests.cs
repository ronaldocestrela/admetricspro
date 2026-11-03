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
using SocialPublishing.Domain.Planning;
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

        var postRepository = new OrganicPostRepository(accessor);
        var actCountConnections = () => connectionRepository.CountByWorkspaceAsync(Guid.NewGuid(), cts.Token);
        var actCountPosts = () => postRepository.CountByWorkspaceAndStatusAsync(
            Guid.NewGuid(),
            OrganicPostStatus.Scheduled,
            cts.Token);

        await actCountConnections.Should().ThrowAsync<OperationCanceledException>();
        await actCountPosts.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Valida métodos de contagem por workspace e status agendado.
    /// </summary>
    [Fact]
    public async Task CountMethods_Should_ReturnCorrectTotals()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var connectionRepository = new OrganicConnectionRepository(accessor);
        var postRepository = new OrganicPostRepository(accessor);
        var unitOfWork = new SocialPublishingUnitOfWork(accessor);

        var connection = OrganicConnection.Create(
            workspaceId,
            OrganicPlatform.Instagram,
            "ig-1",
            "Conta",
            "scope",
            "cipher").Value;
        await connectionRepository.AddAsync(connection, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var draftPost = OrganicPost.Create(
            workspaceId,
            connection.Id,
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            "Draft post",
            new DateTime(2026, 6, 2, 10, 0, 0),
            "America/Sao_Paulo",
            new List<(int Width, int Height, int SortOrder)> { (1080, 1350, 0) }).Value;

        var scheduledPost = OrganicPost.Create(
            workspaceId,
            connection.Id,
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Sale,
            "Scheduled post",
            new DateTime(2026, 6, 3, 10, 0, 0),
            "America/Sao_Paulo",
            new List<(int Width, int Height, int SortOrder)> { (1080, 1350, 0) }).Value;
        scheduledPost.SubmitForApproval().IsSuccess.Should().BeTrue();
        scheduledPost.Approve().IsSuccess.Should().BeTrue();
        scheduledPost.Schedule().IsSuccess.Should().BeTrue();

        await postRepository.AddAsync(draftPost, CancellationToken.None);
        await postRepository.AddAsync(scheduledPost, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        (await connectionRepository.CountByWorkspaceAsync(workspaceId, CancellationToken.None)).Should().Be(1);
        (await postRepository.CountByWorkspaceAndStatusAsync(workspaceId, OrganicPostStatus.Scheduled, CancellationToken.None))
            .Should().Be(1);
        (await postRepository.CountByWorkspaceAndStatusAsync(workspaceId, OrganicPostStatus.Draft, CancellationToken.None))
            .Should().Be(1);
    }

    /// <summary>
    /// Valida round-trip de mídia standalone com object key e ausência de coluna binária.
    /// </summary>
    [Fact]
    public async Task OrganicMediaAssetRepository_Should_PersistMetadataWithoutBinary()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var mediaRepository = new OrganicMediaAssetRepository(accessor);
        var unitOfWork = new SocialPublishingUnitOfWork(accessor);

        const string objectKey = "organic-media/durable/test/key";
        var asset = OrganicMediaAsset.Register(
            workspaceId,
            OrganicPostFormat.FeedImage,
            OrganicMediaMimeTypes.Jpeg,
            null,
            1080,
            1080,
            objectKey).Value;

        await mediaRepository.AddAsync(asset, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var reloaded = await mediaRepository.GetByIdAsync(asset.Id, CancellationToken.None);
        reloaded.Should().NotBeNull();
        reloaded!.ObjectKey.Should().Be(objectKey);
        reloaded.AspectLabel.Should().Be(AspectRatioRules.Label11);

        await using var context = new SocialPublishingDbContext(SocialPublishingDbContext.CreateOptions(connectionString));
        var columns = context.Model.FindEntityType(typeof(OrganicMediaAsset))!.GetProperties().Select(p => p.Name).ToList();
        columns.Should().NotContain(name => name.Contains("Binary", StringComparison.OrdinalIgnoreCase));
        columns.Should().NotContain(name => name.Contains("Content", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Valida round-trip de Revise no brand voice com o mesmo identificador.
    /// </summary>
    [Fact]
    public async Task BrandVoiceRepository_Revise_Should_PersistUpdatedFieldsWithSameId()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var brandVoiceRepository = new BrandVoiceRepository(accessor);
        var unitOfWork = new SocialPublishingUnitOfWork(accessor);

        var profile = BrandVoiceProfile.Create(
            workspaceId,
            "Tom inicial",
            "Persona inicial",
            ["https://example.com/start"],
            ["inicial"],
            EmojiPolicy.Allowed,
            10,
            40,
            20,
            25,
            15).Value;

        await brandVoiceRepository.AddAsync(profile, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var loaded = await brandVoiceRepository.GetByWorkspaceAsync(workspaceId, CancellationToken.None);
        loaded.Should().NotBeNull();
        var id = loaded!.Id;

        loaded.Revise(
            "Tom atualizado",
            "Persona atualizada",
            ["https://example.com/updated"],
            ["atualizado"],
            EmojiPolicy.Forbidden,
            20,
            50,
            10,
            20,
            20,
            "en-US").IsSuccess.Should().BeTrue();

        await brandVoiceRepository.UpdateAsync(loaded, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var reloaded = await brandVoiceRepository.GetByWorkspaceAsync(workspaceId, CancellationToken.None);
        reloaded.Should().NotBeNull();
        reloaded!.Id.Should().Be(id);
        reloaded.Tone.Should().Be("Tom atualizado");
        reloaded.PillarWeights.Authority.Should().Be(50);
        reloaded.MandatoryLinks.Should().ContainSingle().Which.Should().Be("https://example.com/updated");
        reloaded.ForbiddenTerms.Should().Contain("atualizado");
    }

    /// <summary>
    /// Valida chave única de plano editorial e listagem por intervalo UTC de posts.
    /// </summary>
    [Fact]
    public async Task EditorialPlanGeneration_AndScheduledRange_Should_EnforceUniqueKeyAndFilterPosts()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var planRepository = new EditorialPlanGenerationRepository(accessor);
        var postRepository = new OrganicPostRepository(accessor);
        var unitOfWork = new SocialPublishingUnitOfWork(accessor);

        var connection = OrganicConnection.Create(
            workspaceId,
            OrganicPlatform.Instagram,
            "ig-plan",
            "Conta",
            "scope",
            "cipher").Value;
        await new OrganicConnectionRepository(accessor).AddAsync(connection, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var inside = OrganicPost.CreateEditorialDraft(
            workspaceId,
            connection.Id,
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            "Dentro",
            new DateTime(2026, 11, 3, 10, 0, 0),
            "America/Sao_Paulo").Value;

        var outside = OrganicPost.CreateEditorialDraft(
            workspaceId,
            connection.Id,
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Sale,
            "Fora",
            new DateTime(2026, 12, 1, 10, 0, 0),
            "America/Sao_Paulo").Value;

        await postRepository.AddAsync(inside, CancellationToken.None);
        await postRepository.AddAsync(outside, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var from = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 11, 30, 23, 59, 59, DateTimeKind.Utc);
        var ranged = await postRepository.ListByWorkspaceScheduledRangeAsync(workspaceId, from, to, CancellationToken.None);
        ranged.Should().ContainSingle();
        ranged[0].Id.Should().Be(inside.Id);

        var generation = EditorialPlanGeneration.Start(
            workspaceId,
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 6),
            seed: 0).Value;
        generation.RecordCreatedPost(inside.Id).IsSuccess.Should().BeTrue();
        generation.Complete().IsSuccess.Should().BeTrue();

        await planRepository.AddAsync(generation, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var duplicate = EditorialPlanGeneration.Start(
            workspaceId,
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 6),
            seed: 0).Value;

        var act = async () =>
        {
            await planRepository.AddAsync(duplicate, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        };

        await act.Should().ThrowAsync<Exception>();

        var reloaded = await planRepository.GetByKeyAsync(
            workspaceId,
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 6),
            0,
            CancellationToken.None);
        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(EditorialPlanGenerationStatus.Completed);
        reloaded.CreatedPostIds.Should().ContainSingle().Which.Should().Be(inside.Id);
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
