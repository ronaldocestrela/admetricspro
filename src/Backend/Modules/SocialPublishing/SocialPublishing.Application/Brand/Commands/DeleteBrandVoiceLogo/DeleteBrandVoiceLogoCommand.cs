using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Brand.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Brand.Commands.DeleteBrandVoiceLogo;

/// <summary>
/// Remove a logo do cliente do Brand Voice do workspace.
/// </summary>
/// <param name="WorkspaceId">Workspace do cliente.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record DeleteBrandVoiceLogoCommand(Guid WorkspaceId) : ICommand<BrandVoiceDto>;