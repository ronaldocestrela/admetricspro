using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Planning.Commands.GenerateEditorialPlan;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Jobs.Commands.AcceptEditorialPlanGeneration;

/// <summary>
/// Aceita materialização assíncrona de plano editorial (sem chamada à IA no request HTTP).
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="RangeStart">Data inicial inclusiva no fuso informado.</param>
/// <param name="RangeEnd">Data final inclusiva no fuso informado.</param>
/// <param name="TimeZoneId">Identificador IANA do fuso do workspace.</param>
/// <param name="PostsPerWeek">Quantidade de posts por semana (1–7).</param>
/// <param name="Seed">Semente determinística do planner.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record AcceptEditorialPlanGenerationCommand(
    Guid WorkspaceId,
    DateOnly RangeStart,
    DateOnly RangeEnd,
    string TimeZoneId,
    int PostsPerWeek,
    int Seed) : ICommand<UserInitiatedJobDto>;