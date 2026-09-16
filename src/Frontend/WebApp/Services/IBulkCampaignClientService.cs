using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.DTOs;

namespace WebApp.Services;

/// <summary>
/// Contrato do cliente HTTP para operações em massa e edição rápida de campanhas multiplataforma.
/// Consome a Web API de forma estritamente desacoplada da infraestrutura de banco de dados.
/// </summary>
public interface IBulkCampaignClientService
{
    /// <summary>
    /// Obtém as campanhas disponíveis no workspace para exibição e seleção na matriz de edição em massa.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="platform">Filtro opcional por plataforma (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
    /// <param name="status">Filtro opcional por status.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de campanhas normalizadas com hierarquia básica.</returns>
    Task<Result<IReadOnlyList<CampaignHierarchyDto>>> GetCampaignsForBulkAsync(
        Guid workspaceId,
        string? platform = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia um lote de operações (ativar, pausar ou reajustar orçamento) para a Web API.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace associado.</param>
    /// <param name="operations">Coleção de operações a serem aplicadas no lote.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado consolidado com itens alterados com sucesso e eventuais falhas parciais.</returns>
    Task<Result<BulkCampaignOperationResultDto>> ExecuteBulkOperationsAsync(
        Guid workspaceId,
        IReadOnlyList<BulkCampaignOperationItem> operations,
        CancellationToken cancellationToken = default);
}
