using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Domain.Campaigns.Mappers;

/// <summary>
/// Contrato para o conversor de respostas nativas da Google Ads API
/// para o modelo unificado de hierarquia.
/// </summary>
public interface IGoogleAdsHierarchyMapper
{
    /// <summary>
    /// Converte respostas JSON estruturadas do Google Ads API searchStream em hierarquia unificada.
    /// </summary>
    /// <param name="googleRowsJson">JSON contendo as linhas retornadas pela consulta GAQL.</param>
    /// <param name="defaultCurrency">Moeda padrão da conta.</param>
    /// <returns>Hierarquia unificada normalizada ou erro de conversão.</returns>
    Result<UnifiedCampaignHierarchy> Map(
        string? googleRowsJson,
        string defaultCurrency = "BRL");
}
