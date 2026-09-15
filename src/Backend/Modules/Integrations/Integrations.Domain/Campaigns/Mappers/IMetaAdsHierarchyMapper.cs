using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Domain.Campaigns.Mappers;

/// <summary>
/// Contrato para o conversor de estruturas nativas da Meta Ads Graph API v21.0
/// para o modelo unificado de hierarquia.
/// </summary>
public interface IMetaAdsHierarchyMapper
{
    /// <summary>
    /// Converte payloads JSON brutos ou estruturados da Meta Graph API em hierarquia unificada.
    /// </summary>
    /// <param name="campaignsJson">JSON bruto retornado pelo endpoint /campaigns da Meta.</param>
    /// <param name="adSetsJson">JSON bruto retornado pelo endpoint /adsets da Meta.</param>
    /// <param name="adsJson">JSON bruto retornado pelo endpoint /ads da Meta.</param>
    /// <param name="defaultCurrency">Moeda padrão da conta conectada.</param>
    /// <returns>Hierarquia unificada normalizada ou erro de conversão.</returns>
    Result<UnifiedCampaignHierarchy> Map(
        string? campaignsJson,
        string? adSetsJson,
        string? adsJson,
        string defaultCurrency = "BRL");
}
