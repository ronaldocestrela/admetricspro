using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Domain.Campaigns.Mappers;

/// <summary>
/// Contrato para o conversor de respostas nativas da Microsoft Advertising / Bing Ads API
/// para o modelo unificado de hierarquia.
/// </summary>
public interface IBingAdsHierarchyMapper
{
    /// <summary>
    /// Converte respostas JSON da Bing Ads API em hierarquia unificada.
    /// </summary>
    /// <param name="campaignsJson">JSON retornado para campanhas do Bing.</param>
    /// <param name="adGroupsJson">JSON retornado para grupos de anúncios do Bing.</param>
    /// <param name="adsJson">JSON retornado para anúncios do Bing.</param>
    /// <param name="defaultCurrency">Moeda da conta.</param>
    /// <returns>Hierarquia unificada normalizada ou erro de conversão.</returns>
    Result<UnifiedCampaignHierarchy> Map(
        string? campaignsJson,
        string? adGroupsJson,
        string? adsJson,
        string defaultCurrency = "BRL");
}
