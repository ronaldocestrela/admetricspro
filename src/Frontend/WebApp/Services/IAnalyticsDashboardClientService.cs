using Analytics.Application.Dashboard.DTOs;
using BuildingBlocks.Domain.Primitives;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Contrato de serviço cliente para consumo dos endpoints de inteligência e dashboard executivo da Web API.
/// </summary>
public interface IAnalyticsDashboardClientService
{
    /// <summary>
    /// Consulta o resumo executivo unificado cross-network com comparação temporal e deltas com base nos filtros informados.
    /// </summary>
    /// <param name="filters">Filtros globais selecionados (workspace, canal, período, dispositivo).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo os dados consolidados do painel ou falha da API.</returns>
    Task<Result<ExecutiveDashboardDto>> GetExecutiveDashboardAsync(
        DashboardFiltersState filters,
        CancellationToken cancellationToken = default);
}
