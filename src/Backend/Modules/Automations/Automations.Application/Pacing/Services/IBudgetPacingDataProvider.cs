using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.Pacing.Services;

/// <summary>
/// Dados brutos de gasto e orçamento de uma campanha no ciclo analisado.
/// </summary>
public sealed record CampaignSpendRawData(
    Guid CampaignId,
    string CampaignName,
    string Platform,
    decimal? DailyBudget,
    decimal CurrentSpend);

/// <summary>
/// Dados brutos consolidados de gasto acumulado e orçamento contratado de um Workspace.
/// </summary>
public sealed record WorkspacePacingRawData(
    Guid WorkspaceId,
    string WorkspaceName,
    decimal MonthlyAdSpendBudget,
    decimal CurrentSpend,
    IReadOnlyList<CampaignSpendRawData> Campaigns);

/// <summary>
/// Provedor de dados brutos de consumo e orçamento do banco do inquilino para cálculos de pacing.
/// </summary>
public interface IBudgetPacingDataProvider
{
    /// <summary>
    /// Consulta os dados brutos de orçamento e investimento de um workspace no intervalo especificado.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="startDateUtc">Data inicial do ciclo em UTC.</param>
    /// <param name="endDateUtc">Data final do ciclo em UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com os dados do workspace ou erro de negócio.</returns>
    Task<Result<WorkspacePacingRawData>> GetWorkspacePacingDataAsync(
        Guid workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta os dados brutos de orçamento e investimento de toda a carteira de clientes do inquilino.
    /// </summary>
    /// <param name="squadId">Filtro opcional por Squad.</param>
    /// <param name="startDateUtc">Data inicial do ciclo em UTC.</param>
    /// <param name="endDateUtc">Data final do ciclo em UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com a lista de dados dos workspaces da carteira.</returns>
    Task<Result<IReadOnlyList<WorkspacePacingRawData>>> GetPortfolioPacingDataAsync(
        Guid? squadId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default);
}
