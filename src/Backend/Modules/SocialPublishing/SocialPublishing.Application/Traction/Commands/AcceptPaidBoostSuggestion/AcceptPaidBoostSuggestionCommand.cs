using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Traction.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Traction.Commands.AcceptPaidBoostSuggestion;

/// <summary>
/// Aceita uma sugestão de impulsionamento e solicita rascunho de anúncio pago ao módulo Integrations.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="SuggestionId">Identificador da sugestão.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record AcceptPaidBoostSuggestionCommand(Guid WorkspaceId, Guid SuggestionId)
    : ICommand<PaidBoostSuggestionDto>;
