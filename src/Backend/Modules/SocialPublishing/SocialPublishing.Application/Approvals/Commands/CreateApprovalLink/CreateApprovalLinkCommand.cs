using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Approvals.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Approvals.Commands.CreateApprovalLink;

/// <summary>
/// Emite um link compartilhável de aprovação orgânica para um workspace.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário do pacote.</param>
/// <param name="ExpiresAtUtc">Expiração obrigatória em UTC.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record CreateApprovalLinkCommand(
    Guid WorkspaceId,
    DateTime ExpiresAtUtc) : ICommand<ApprovalLinkCreatedDto>;
