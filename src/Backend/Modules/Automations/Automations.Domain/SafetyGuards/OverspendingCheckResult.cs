using BuildingBlocks.Domain.Automations.SafetyGuards;

namespace Automations.Domain.SafetyGuards;

/// <summary>
/// Resultado da execução da trava de segurança de Overspending em um Workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace verificado.</param>
/// <param name="EvaluatedCampaignsCount">Total de campanhas ativas avaliadas com orçamento configurado.</param>
/// <param name="ViolatedCampaignsCount">Quantidade de campanhas que superaram o limiar de 120% do orçamento diário.</param>
/// <param name="PausedCampaignsCount">Quantidade de campanhas pausadas com sucesso via comando in-memory.</param>
/// <param name="Incidents">Coleção de incidentes gerados e registrados para auditoria.</param>
public sealed record OverspendingCheckResult(
    Guid WorkspaceId,
    int EvaluatedCampaignsCount,
    int ViolatedCampaignsCount,
    int PausedCampaignsCount,
    IReadOnlyList<SafetyGuardIncident> Incidents);
