using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Traction.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Traction.Queries.ListPaidBoostSuggestions;

/// <summary>
/// Lista sugestões de impulsionamento de um workspace.
/// </summary>
/// <param name="WorkspaceId">Workspace filtrado.</param>
[RequireTenantPermission(TenantPermission.SocialRead, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record ListPaidBoostSuggestionsQuery(Guid WorkspaceId) : IQuery<IReadOnlyList<PaidBoostSuggestionDto>>;
