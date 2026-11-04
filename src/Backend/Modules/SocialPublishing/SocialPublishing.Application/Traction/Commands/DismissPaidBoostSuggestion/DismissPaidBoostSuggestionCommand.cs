using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Traction.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Traction.Commands.DismissPaidBoostSuggestion;

/// <summary>
/// Dispensa uma sugestão de impulsionamento sem solicitar rascunho de anúncio pago.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="SuggestionId">Identificador da sugestão.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record DismissPaidBoostSuggestionCommand(Guid WorkspaceId, Guid SuggestionId)
    : ICommand<PaidBoostSuggestionDto>;
