using FluentAssertions;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.MultiTenancy;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Traction;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Approvals;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Domain.Publications;
using Tenants.Application.Rbac.Attributes;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Infraestrutura compartilhada para testes de comandos de ciclo de vida de posts orgânicos.
/// </summary>
internal sealed class OrganicPostLifecycleCommandTestHost
{
    public OrganicPostLifecycleCommandTestHost()
    {
        WorkspaceId = Guid.NewGuid();
        UserId = Guid.NewGuid();
        TenantContextAccessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(Guid.NewGuid(), "test", TenantResolutionSource.Header)
        };
        CurrentUserContext = Substitute.For<ICurrentUserContext>();
        CurrentUserContext.IsAuthenticated.Returns(true);
        CurrentUserContext.UserId.Returns(UserId);
        PostRepository = new InMemoryOrganicPostRepository();
        PublicationJobRepository = new InMemoryPublicationJobRepository();
        UnitOfWork = Substitute.For<ISocialPublishingUnitOfWork>();
        FixedTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        TimeProvider = Substitute.For<TimeProvider>();
        TimeProvider.GetUtcNow().Returns(FixedTime);
    }

    /// <summary>
    /// Obtém o workspace padrão dos testes.
    /// </summary>
    public Guid WorkspaceId { get; }

    /// <summary>
    /// Obtém o usuário autenticado simulado.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Obtém o repositório em memória.
    /// </summary>
    public InMemoryOrganicPostRepository PostRepository { get; }

    /// <summary>
    /// Obtém o repositório de jobs em memória.
    /// </summary>
    public InMemoryPublicationJobRepository PublicationJobRepository { get; }

    /// <summary>
    /// Obtém o unit of work substituído.
    /// </summary>
    public ISocialPublishingUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Obtém o accessor de tenant.
    /// </summary>
    public ITenantContextAccessor TenantContextAccessor { get; }

    /// <summary>
    /// Obtém o contexto de usuário.
    /// </summary>
    public ICurrentUserContext CurrentUserContext { get; }

    /// <summary>
    /// Obtém o relógio fixo.
    /// </summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>
    /// Obtém o instante UTC fixo retornado pelo relógio.
    /// </summary>
    public DateTimeOffset FixedTime { get; }

    /// <summary>
    /// Valida atributo de permissão em um comando.
    /// </summary>
    /// <typeparam name="TCommand">Tipo do comando.</typeparam>
    public static void AssertSocialCopilotPermission<TCommand>()
        where TCommand : class
    {
        var attribute = typeof(TCommand).GetCustomAttributes(typeof(RequireTenantPermissionAttribute), false)
            .Cast<RequireTenantPermissionAttribute>()
            .Single();
        attribute.Permission.Should().Be(TenantPermission.SocialCopilotGenerate);
    }

    /// <summary>
    /// Cria um post Draft no repositório.
    /// </summary>
    /// <param name="workspaceId">Workspace; usa o padrão se vazio.</param>
    /// <returns>Post persistido em memória.</returns>
    public OrganicPost SeedDraftPost(Guid? workspaceId = null)
    {
        var ws = workspaceId ?? WorkspaceId;
        var post = OrganicPost.CreateEditorialDraft(
            ws,
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            global::SocialPublishing.Domain.Brand.ContentPillar.Authority,
            "Legenda",
            new DateTime(2026, 6, 2, 10, 0, 0),
            "America/Sao_Paulo").Value;
        PostRepository.AllPosts.Add(post);
        return post;
    }

    /// <summary>
    /// Avança o post até o status informado.
    /// </summary>
    /// <param name="post">Post de origem.</param>
    /// <param name="status">Status alvo.</param>
    public static void AdvanceTo(OrganicPost post, OrganicPostStatus status)
    {
        while (post.Status != status)
        {
            var step = post.Status switch
            {
                OrganicPostStatus.Draft => post.SubmitForApproval(),
                OrganicPostStatus.PendingApproval => post.Approve(),
                OrganicPostStatus.Approved => post.Schedule(),
                OrganicPostStatus.Scheduled => post.MarkPublishing(),
                OrganicPostStatus.Publishing => post.MarkPublished("test-external-id"),
                _ => throw new InvalidOperationException($"Cannot advance from {post.Status} to {status}.")
            };

            step.IsSuccess.Should().BeTrue();
        }
    }
}

/// <summary>
/// Repositório em memória para testes de posts orgânicos.
/// </summary>
internal sealed class InMemoryOrganicPostRepository : IOrganicPostRepository
{
    /// <summary>
    /// Posts armazenados.
    /// </summary>
    public List<OrganicPost> AllPosts { get; } = new();

    /// <inheritdoc />
    public Task<OrganicPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(AllPosts.FirstOrDefault(p => p.Id == id));

    /// <inheritdoc />
    public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganicPost>>(AllPosts.Where(p => p.WorkspaceId == workspaceId).ToList());

    /// <inheritdoc />
    public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceScheduledRangeAsync(
        Guid workspaceId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganicPost>>(AllPosts.Where(p => p.WorkspaceId == workspaceId).ToList());

    /// <inheritdoc />
    public Task AddAsync(OrganicPost post, CancellationToken cancellationToken = default)
    {
        AllPosts.Add(post);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<int> CountByWorkspaceAndStatusAsync(
        Guid workspaceId,
        OrganicPostStatus status,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AllPosts.Count(p => p.WorkspaceId == workspaceId && p.Status == status));

    /// <inheritdoc />
    public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAndStatusAsync(
        Guid workspaceId,
        OrganicPostStatus status,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganicPost>>(
            AllPosts.Where(p => p.WorkspaceId == workspaceId && p.Status == status).ToList());

    /// <inheritdoc />
    public Task<IReadOnlyList<OrganicPostTractionCandidate>> ListPublishedForTractionAsync(
        DateTime publishedSinceUtc,
        CancellationToken cancellationToken = default)
    {
        if (TractionCandidates.Count > 0)
        {
            return Task.FromResult<IReadOnlyList<OrganicPostTractionCandidate>>(
                TractionCandidates.Where(c => c.PublishedAtUtc >= publishedSinceUtc).ToList());
        }

        var fromPosts = AllPosts
            .Where(p =>
                p.Status == OrganicPostStatus.Published &&
                p.ScheduledAtUtc >= publishedSinceUtc &&
                !string.IsNullOrWhiteSpace(p.PublishedExternalId))
            .Select(p =>
            {
                var media = p.Media.OrderBy(m => m.SortOrder).FirstOrDefault();
                return new OrganicPostTractionCandidate(
                    p.Id,
                    p.WorkspaceId,
                    p.ConnectionId,
                    p.Platform,
                    p.ScheduledAtUtc,
                    p.PublishedExternalId!,
                    media?.ObjectKey);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<OrganicPostTractionCandidate>>(fromPosts);
    }

    /// <summary>
    /// Candidatos explícitos para testes de scan de tração.
    /// </summary>
    public List<OrganicPostTractionCandidate> TractionCandidates { get; } = new();
}

/// <summary>
/// Repositório em memória para testes de links de aprovação.
/// </summary>
internal sealed class InMemoryApprovalLinkRepository : IApprovalLinkRepository
{
    /// <summary>
    /// Links armazenados.
    /// </summary>
    public List<ApprovalLink> AllLinks { get; } = new();

    /// <inheritdoc />
    public Task<ApprovalLink?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(AllLinks.FirstOrDefault(l => l.TokenHash == tokenHash));

    /// <inheritdoc />
    public Task AddAsync(ApprovalLink link, CancellationToken cancellationToken = default)
    {
        AllLinks.Add(link);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(ApprovalLink link, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<int> RevokeActiveByWorkspaceAsync(
        Guid workspaceId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var link in AllLinks.Where(l => l.WorkspaceId == workspaceId && l.RevokedAtUtc is null))
        {
            link.Revoke(revokedAtUtc);
            count++;
        }

        return Task.FromResult(count);
    }
}

/// <summary>
/// Repositório em memória para testes da outbox de publicação.
/// </summary>
internal sealed class InMemoryPublicationJobRepository : IPublicationJobRepository
{
    private readonly object _claimLock = new();

    /// <summary>
    /// Jobs armazenados.
    /// </summary>
    public List<PublicationJob> AllJobs { get; } = new();

    /// <inheritdoc />
    public Task<PublicationJob?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(AllJobs.FirstOrDefault(j => j.IdempotencyKey == idempotencyKey));

    /// <inheritdoc />
    public Task<PublicationJob?> GetActiveByPostIdAsync(Guid postId, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            AllJobs.FirstOrDefault(j =>
                j.PostId == postId &&
                j.Status is PublicationJobStatus.Pending or PublicationJobStatus.RetryScheduled or PublicationJobStatus.InProgress));

    /// <inheritdoc />
    public Task<Result> AddAsync(PublicationJob job, CancellationToken cancellationToken = default)
    {
        if (AllJobs.Any(j => j.IdempotencyKey == job.IdempotencyKey))
        {
            return Task.FromResult(Result.Failure(SocialErrors.PublicationJobAlreadyExists));
        }

        AllJobs.Add(job);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task UpdateAsync(PublicationJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<PublicationJob?> TryClaimAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        lock (_claimLock)
        {
            var job = AllJobs.FirstOrDefault(j => j.Id == jobId);
            if (job is null || !job.CanBeClaimed)
            {
                return Task.FromResult<PublicationJob?>(null);
            }

            job.MarkInProgress().IsSuccess.Should().BeTrue();
            return Task.FromResult<PublicationJob?>(job);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PublicationJob>> ListDueAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var due = AllJobs.Where(j =>
                (j.Status == PublicationJobStatus.Pending && j.ScheduledAtUtc <= utcNow) ||
                (j.Status == PublicationJobStatus.RetryScheduled && j.NextAttemptAtUtc != null && j.NextAttemptAtUtc <= utcNow))
            .OrderBy(j => j.ScheduledAtUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<PublicationJob>>(due);
    }
}
