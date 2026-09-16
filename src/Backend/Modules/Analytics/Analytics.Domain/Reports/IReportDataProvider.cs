using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Reports;

/// <summary>
/// Contrato do provedor de dados consolidado para compilação de relatórios executivos.
/// </summary>
public interface IReportDataProvider
{
    /// <summary>
    /// Consulta as métricas reais do banco do inquilino, calcula blended metrics, obtém criativos e diagnósticos
    /// e compõe o modelo de renderização aplicando a identidade visual do Tenant.
    /// </summary>
    /// <param name="workspaceId">Identificador do Workspace avaliado.</param>
    /// <param name="startDateUtc">Data de início do período das métricas.</param>
    /// <param name="endDateUtc">Data de término do período das métricas.</param>
    /// <param name="customTitle">Título opcional definido pelo gestor.</param>
    /// <param name="customNotes">Observações estratégicas adicionadas pelo gestor.</param>
    /// <param name="includeCopilot">Se verdadeiro, inclui os diagnósticos do Copiloto de IA.</param>
    /// <param name="includeCreatives">Se verdadeiro, inclui a vitrine de criativos.</param>
    /// <param name="includeChannels">Se verdadeiro, inclui o detalhamento por plataforma.</param>
    /// <param name="includePacing">Se verdadeiro, inclui as métricas de velocidade orçamentária.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Modelo de renderização do relatório ou falha.</returns>
    Task<Result<ReportRenderModel>> BuildReportModelAsync(
        Guid workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        string? customTitle,
        string? customNotes,
        bool includeCopilot,
        bool includeCreatives,
        bool includeChannels,
        bool includePacing,
        CancellationToken cancellationToken = default);
}
