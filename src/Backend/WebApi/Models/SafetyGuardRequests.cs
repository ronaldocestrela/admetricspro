using System.ComponentModel.DataAnnotations;
using Automations.Domain.SafetyGuards;

namespace WebApi.Models;

/// <summary>
/// Modelo de requisição para execução da trava de Overspending em um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace.</param>
/// <param name="ThresholdMultiplier">Multiplicador opcional do limiar de estouro (padrão: 1.20 = 120%).</param>
public sealed record CheckOverspendingApiRequest(
    [Required] Guid WorkspaceId,
    decimal ThresholdMultiplier = IOverspendingGuard.DefaultOverspendingThreshold);

/// <summary>
/// Modelo de requisição para execução da verificação de saúde das Landing Pages em um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace.</param>
public sealed record CheckLandingPagesApiRequest(
    [Required] Guid WorkspaceId);
