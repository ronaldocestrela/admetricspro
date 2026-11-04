using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Posts.Queries.ListOrganicPosts;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using Tenants.Application.Rbac.Attributes;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes da consulta <see cref="ListOrganicPostsQueryHandler"/>.
/// </summary>
public sealed class ListOrganicPostsQueryTests
{
    /// <summary>
    /// A consulta exige permissão social.read no contrato MediatR.
    /// </summary>
    [Fact]
    public void Query_DeveExigirPermissaoSocialRead()
    {
        var attribute = typeof(ListOrganicPostsQuery)
            .GetCustomAttributes(typeof(RequireTenantPermissionAttribute), inherit: true)
            .Cast<RequireTenantPermissionAttribute>()
            .Single();

        attribute.Permission.Should().Be(TenantPermission.SocialRead);
    }

    /// <summary>
    /// Filtra posts pelo intervalo UTC inclusivo e ignora fora da janela.
    /// </summary>
    [Fact]
    public async Task Handle_ComIntervaloUtc_DeveRetornarSomentePostsNaJanela()
    {
        var workspaceId = Guid.NewGuid();
        var connectionId = Guid.NewGuid();
        var inside = CreatePost(workspaceId, connectionId, new DateTime(2026, 11, 3, 10, 0, 0));
        var outside = CreatePost(workspaceId, connectionId, new DateTime(2026, 12, 1, 10, 0, 0));

        var repository = new FakePostRepository([inside, outside]);
        var handler = new ListOrganicPostsQueryHandler(repository);

        var from = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 11, 30, 23, 59, 59, DateTimeKind.Utc);

        var result = await handler.Handle(new ListOrganicPostsQuery(workspaceId, from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Id.Should().Be(inside.Id);
    }

    /// <summary>
    /// Limite superior anterior ao inferior retorna PostsRangeInvalid.
    /// </summary>
    [Fact]
    public async Task Handle_ComToAnteriorAoFrom_DeveRetornarPostsRangeInvalid()
    {
        var handler = new ListOrganicPostsQueryHandler(new FakePostRepository([]));
        var from = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await handler.Handle(new ListOrganicPostsQuery(Guid.NewGuid(), from, to), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PostsRangeInvalid.Code);
    }

    private static OrganicPost CreatePost(Guid workspaceId, Guid connectionId, DateTime localScheduledAt)
    {
        return OrganicPost.CreateEditorialDraft(
            workspaceId,
            connectionId,
            OrganicPlatform.Instagram,
            OrganicPostFormat.FeedImage,
            ContentPillar.Authority,
            "Legenda",
            localScheduledAt,
            "America/Sao_Paulo").Value;
    }

    private sealed class FakePostRepository(IReadOnlyList<OrganicPost> posts) : IOrganicPostRepository
    {
        public Task<OrganicPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(posts.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(posts.Where(p => p.WorkspaceId == workspaceId).ToList());

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceScheduledRangeAsync(
            Guid workspaceId,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(
                posts
                    .Where(p => p.WorkspaceId == workspaceId &&
                                p.ScheduledAtUtc >= fromUtc &&
                                p.ScheduledAtUtc <= toUtc)
                    .OrderBy(p => p.ScheduledAtUtc)
                    .ToList());

        public Task AddAsync(OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(OrganicPost post, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountByWorkspaceAndStatusAsync(
            Guid workspaceId,
            OrganicPostStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(posts.Count(p => p.WorkspaceId == workspaceId && p.Status == status));

        public Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAndStatusAsync(
            Guid workspaceId,
            OrganicPostStatus status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganicPost>>(
                posts.Where(p => p.WorkspaceId == workspaceId && p.Status == status).ToList());
    }
}
