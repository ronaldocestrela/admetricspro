using FluentAssertions;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.MultiTenancy;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
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
                OrganicPostStatus.Publishing => post.MarkPublished(),
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
}
