using FluentAssertions;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Domain.Publications;
using SocialPublishing.Infrastructure.Persistence;
using SocialPublishing.Infrastructure.Persistence.Repositories;

namespace IntegrationTests.SocialPublishing;

/// <summary>
/// Testes de integração do repositório de outbox de publicação orgânica.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class PublicationJobRepositoryTests(SqlServerFixture fixture)
{
    /// <summary>
    /// Post e job persistidos no mesmo commit e chave recarregável.
    /// </summary>
    [Fact]
    public async Task AddAsync_ComPostNoMesmoCommit_DevePersistirJob()
    {
        var tenantId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var postRepository = new OrganicPostRepository(accessor);
        var jobRepository = new PublicationJobRepository(accessor);
        var unitOfWork = new SocialPublishingUnitOfWork(accessor);

        var workspaceId = Guid.NewGuid();
        var post = OrganicPost.CreateEditorialDraft(
            workspaceId,
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            global::SocialPublishing.Domain.Brand.ContentPillar.Authority,
            "Legenda",
            new DateTime(2026, 6, 2, 10, 0, 0),
            "America/Sao_Paulo").Value;
        post.SubmitForApproval().IsSuccess.Should().BeTrue();
        post.Approve().IsSuccess.Should().BeTrue();
        post.Schedule().IsSuccess.Should().BeTrue();

        var key = PublicationJobKey.Build(post.Id, post.ScheduledAtUtc);
        var job = PublicationJob.Open(post.Id, workspaceId, post.ScheduledAtUtc, DateTime.UtcNow).Value;

        await postRepository.AddAsync(post, CancellationToken.None);
        (await jobRepository.AddAsync(job, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await unitOfWork.CommitAsync(CancellationToken.None);

        var reloaded = await jobRepository.GetByIdempotencyKeyAsync(key, CancellationToken.None);
        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(PublicationJobStatus.Pending);
    }

    /// <summary>
    /// Reagendamento cancela job anterior e abre chave nova.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_Reagendamento_DeveCancelarEAbrNovaChave()
    {
        var tenantId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var jobRepository = new PublicationJobRepository(accessor);
        var unitOfWork = new SocialPublishingUnitOfWork(accessor);

        var postId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var originalUtc = new DateTime(2026, 7, 1, 14, 0, 0, DateTimeKind.Utc);
        var oldJob = PublicationJob.Open(postId, workspaceId, originalUtc, DateTime.UtcNow).Value;
        (await jobRepository.AddAsync(oldJob, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await unitOfWork.CommitAsync(CancellationToken.None);

        oldJob.Cancel().IsSuccess.Should().BeTrue();
        await jobRepository.UpdateAsync(oldJob, CancellationToken.None);

        var newUtc = originalUtc.AddDays(2);
        var newJob = PublicationJob.Open(postId, workspaceId, newUtc, DateTime.UtcNow).Value;
        (await jobRepository.AddAsync(newJob, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await unitOfWork.CommitAsync(CancellationToken.None);

        var oldKeyJob = await jobRepository.GetByIdempotencyKeyAsync(
            PublicationJobKey.Build(postId, originalUtc),
            CancellationToken.None);
        oldKeyJob!.Status.Should().Be(PublicationJobStatus.Cancelled);

        var newKeyJob = await jobRepository.GetByIdempotencyKeyAsync(
            PublicationJobKey.Build(postId, newUtc),
            CancellationToken.None);
        newKeyJob!.Status.Should().Be(PublicationJobStatus.Pending);
    }

    /// <summary>
    /// Dois claims concorrentes: um vence, o outro recebe null.
    /// </summary>
    [Fact]
    public async Task TryClaimAsync_Concorrente_ApenasUmVence()
    {
        var tenantId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor1 = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var jobRepository1 = new PublicationJobRepository(accessor1);
        var unitOfWork1 = new SocialPublishingUnitOfWork(accessor1);

        var postId = Guid.NewGuid();
        var slotUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        var job = PublicationJob.Open(postId, Guid.NewGuid(), slotUtc, DateTime.UtcNow).Value;
        (await jobRepository1.AddAsync(job, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await unitOfWork1.CommitAsync(CancellationToken.None);

        resolver.SetCurrentTenant(tenantId);
        var accessor2 = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var claimRepo1 = new PublicationJobRepository(accessor1);
        var claimRepo2 = new PublicationJobRepository(accessor2);

        var results = await Task.WhenAll(
            claimRepo1.TryClaimAsync(job.Id, CancellationToken.None),
            claimRepo2.TryClaimAsync(job.Id, CancellationToken.None));

        results.Count(r => r != null).Should().Be(1);
        results.Should().Contain(r => r == null);
        results.Single(r => r != null)!.Status.Should().Be(PublicationJobStatus.InProgress);
    }

    /// <summary>
    /// Job Succeeded não é reivindicado.
    /// </summary>
    [Fact]
    public async Task TryClaimAsync_Succeeded_DeveRetornarNull()
    {
        var tenantId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var jobRepository = new PublicationJobRepository(accessor);
        var unitOfWork = new SocialPublishingUnitOfWork(accessor);

        var job = PublicationJob.Open(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow).Value;
        (await jobRepository.AddAsync(job, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await unitOfWork.CommitAsync(CancellationToken.None);

        var claimed = await jobRepository.TryClaimAsync(job.Id, CancellationToken.None);
        claimed.Should().NotBeNull();

        var tracked = await jobRepository.GetActiveByPostIdAsync(claimed!.PostId, CancellationToken.None);
        tracked!.MarkSucceeded().IsSuccess.Should().BeTrue();
        await jobRepository.UpdateAsync(tracked, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var secondClaim = await jobRepository.TryClaimAsync(job.Id, CancellationToken.None);
        secondClaim.Should().BeNull();
    }

    /// <summary>
    /// Segundo insert com mesma chave retorna conflito.
    /// </summary>
    [Fact]
    public async Task AddAsync_ChaveDuplicada_DeveRetornarPublicationJobAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        var connectionString = await PrepareMigratedDatabaseAsync();
        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string> { [tenantId] = connectionString });
        resolver.SetCurrentTenant(tenantId);

        var accessor = new SocialPublishingDbContextAccessor(new SocialPublishingDbContextFactory(resolver));
        var jobRepository = new PublicationJobRepository(accessor);
        var unitOfWork = new SocialPublishingUnitOfWork(accessor);

        var postId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var slotUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        var first = PublicationJob.Open(postId, workspaceId, slotUtc, DateTime.UtcNow).Value;
        var duplicate = PublicationJob.Open(postId, workspaceId, slotUtc, DateTime.UtcNow).Value;

        (await jobRepository.AddAsync(first, CancellationToken.None)).IsSuccess.Should().BeTrue();
        await unitOfWork.CommitAsync(CancellationToken.None);

        var secondAdd = await jobRepository.AddAsync(duplicate, CancellationToken.None);
        secondAdd.IsFailure.Should().BeTrue();
        secondAdd.Error.Code.Should().Be(SocialErrors.PublicationJobAlreadyExists.Code);

        await using var context = new SocialPublishingDbContext(SocialPublishingDbContext.CreateOptions(connectionString));
        (await context.PublicationJobs.CountAsync()).Should().Be(1);
    }

    private async Task<string> PrepareMigratedDatabaseAsync()
    {
        var databaseName = $"Tenant_SocialOutbox_{Guid.NewGuid():N}";
        var connectionString = SocialPublishingTestDatabase.WithDatabase(fixture.ConnectionString, databaseName);
        await SocialPublishingTestDatabase.EnsureDatabaseCreatedAsync(connectionString);

        var migrator = new SocialPublishingSchemaMigrator(new FakeTenantConnectionResolver(new Dictionary<Guid, string>()));
        (await migrator.ApplyAsync(connectionString, CancellationToken.None)).IsSuccess.Should().BeTrue();
        return connectionString;
    }
}
