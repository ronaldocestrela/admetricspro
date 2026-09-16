using Analytics.Application.Copilot.DTOs;
using BuildingBlocks.Domain.Primitives;

namespace WebApp.Services.Copilot;

/// <summary>
/// Contrato do cliente HTTP tipado para consumo dos endpoints do Copiloto de IA (Auditor de Tráfego).
/// </summary>
public interface ITrafficCopilotClientService
{
    /// <summary>
    /// Obtém o diagnóstico diário sintetizado e a lista de anomalias encontradas no workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="date">Data de referência opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com o relatório de diagnóstico diário.</returns>
    Task<Result<DailyDiagnosticReportDto>> GetDailyDiagnosticAsync(
        Guid workspaceId,
        DateTime? date = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa operacionalmente uma recomendação do Copiloto em 1 clique.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="action">Ação recomendada a ser executada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com a confirmação de execução.</returns>
    Task<Result<ExecuteCopilotActionResultDto>> ExecuteActionAsync(
        Guid workspaceId,
        CopilotRecommendationActionDto action,
        CancellationToken cancellationToken = default);
}
