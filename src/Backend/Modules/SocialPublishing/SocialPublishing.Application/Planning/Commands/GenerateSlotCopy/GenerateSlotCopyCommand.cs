using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Planning.Commands.GenerateSlotCopy;

/// <summary>
/// Comando para gerar legenda e hashtags para um slot editorial (sem persistir post).
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="LocalDate">Data civil do slot no fuso do workspace.</param>
/// <param name="UtcInstant">Instante UTC do slot.</param>
/// <param name="Pillar">Pilar editorial.</param>
/// <param name="Platform">Rede sugerida.</param>
/// <param name="Format">Formato sugerido.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record GenerateSlotCopyCommand(
    Guid WorkspaceId,
    DateOnly LocalDate,
    DateTime UtcInstant,
    ContentPillar Pillar,
    OrganicPlatform Platform,
    OrganicPostFormat Format) : ICommand<GeneratedCopyDto>;
