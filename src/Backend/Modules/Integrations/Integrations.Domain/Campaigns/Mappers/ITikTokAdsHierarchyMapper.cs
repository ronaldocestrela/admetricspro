using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Domain.Campaigns.Mappers;

/// <summary>
/// Contrato para o conversor de respostas nativas da TikTok Marketing API v1.3
/// para o modelo unificado de hierarquia.
/// </summary>
public interface ITikTokAdsHierarchyMapper
{
    /// <summary>
    /// Converte respostas JSON estruturadas da TikTok Marketing API em hierarquia unificada.
    /// </summary>
    /// <param name="campaignsJson">JSON retornado por /campaign/get/.</param>
    /// <param name="adGroupsJson">JSON retornado por /adgroup/get/.</param>
    /// <param name="adsJson">JSON retornado por /ad/get/.</param>
    /// <param name="defaultCurrency">Moeda da conta.</param>
    /// <returns>Hierarquia unificada normalizada ou erro de conversão.</returns>
    Result<UnifiedCampaignHierarchy> Map(
        string? campaignsJson,
        string? adGroupsJson,
        string? adsJson,
        string defaultCurrency = "BRL");
}
