using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Posts.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Posts.Queries.ListOrganicPosts;

/// <summary>
/// Lista posts orgânicos de um workspace em um intervalo UTC de agendamento.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="FromUtc">Limite inferior UTC inclusivo.</param>
/// <param name="ToUtc">Limite superior UTC inclusivo.</param>
[RequireTenantPermission(TenantPermission.SocialRead, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record ListOrganicPostsQuery(
    Guid WorkspaceId,
    DateTime FromUtc,
    DateTime ToUtc) : IQuery<IReadOnlyList<OrganicPostDto>>;
