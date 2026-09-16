using Analytics.Application.Creatives.DTOs;
using BuildingBlocks.Domain.Primitives;

namespace WebApp.Services.Creatives;

/// <summary>
/// Contrato do cliente HTTP tipado para consumo dos endpoints do Creative Hub e Detector de Fadiga da WebAPI.
/// </summary>
public interface ICreativeHubClientService
{
    /// <summary>
    /// Obtém a visão geral do Creative Hub com contadores e lista de criativos monitorados.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="startDate">Data inicial opcional.</param>
    /// <param name="endDate">Data final opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com DTO de visão geral ou erro de requisição.</returns>
    Task<Result<CreativeHubOverviewDto>> GetOverviewAsync(
        Guid workspaceId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o diagnóstico aprofundado de fadiga de um criativo.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="adId">Identificador do anúncio.</param>
    /// <param name="startDate">Data inicial opcional.</param>
    /// <param name="endDate">Data final opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com diagnóstico de fadiga ou erro.</returns>
    Task<Result<CreativeFatigueDto>> GetFatigueAnalysisAsync(
        Guid workspaceId,
        Guid adId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o comparativo de desempenho cross-platform do mesmo ativo no Meta Ads vs. TikTok Ads.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="assetFingerprint">Hash de identificação do ativo de mídia.</param>
    /// <param name="assetName">Nome opcional do criativo.</param>
    /// <param name="previewUrl">URL opcional de preview.</param>
    /// <param name="startDate">Data inicial opcional.</param>
    /// <param name="endDate">Data final opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com relatório comparativo ou erro.</returns>
    Task<Result<CrossPlatformComparisonDto>> GetCrossPlatformComparisonAsync(
        Guid workspaceId,
        string assetFingerprint,
        string? assetName = null,
        string? previewUrl = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
