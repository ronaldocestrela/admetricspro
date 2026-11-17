using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Planning.DTOs;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Planning.Commands.GenerateEditorialPlan;

/// <summary>
/// Materializa um cronograma editorial em posts orgânicos em Draft, com idempotência por intervalo e semente.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="RangeStart">Data inicial inclusiva no fuso informado.</param>
/// <param name="RangeEnd">Data final inclusiva no fuso informado.</param>
/// <param name="TimeZoneId">Identificador IANA do fuso do workspace.</param>
/// <param name="PostsPerWeek">Quantidade de posts por semana (1–7).</param>
/// <param name="Seed">Semente determinística do planner.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record GenerateEditorialPlanCommand(
    Guid WorkspaceId,
    DateOnly RangeStart,
    DateOnly RangeEnd,
    string TimeZoneId,
    int PostsPerWeek,
    int Seed) : ICommand<GeneratedPlanDto>;
