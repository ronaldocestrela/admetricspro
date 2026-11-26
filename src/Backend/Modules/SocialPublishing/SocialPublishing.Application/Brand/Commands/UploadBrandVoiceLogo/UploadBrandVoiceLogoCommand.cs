using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Brand.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Brand.Commands.UploadBrandVoiceLogo;

/// <summary>
/// Envia ou substitui a logo do cliente no Brand Voice do workspace.
/// </summary>
/// <param name="WorkspaceId">Workspace do cliente.</param>
/// <param name="FileName">Nome original do arquivo.</param>
/// <param name="ContentType">MIME declarado.</param>
/// <param name="Content">Conteúdo da imagem.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record UploadBrandVoiceLogoCommand(
    Guid WorkspaceId,
    string FileName,
    string ContentType,
    Stream Content) : ICommand<BrandVoiceDto>;