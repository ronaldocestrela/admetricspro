using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Sync;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>
/// Adaptador de sincronização para contas de demonstração (FTUX).
/// Gera dados estruturais sintéticos de alta fidelidade para aceleração visual imediata.
/// </summary>
public sealed class DemoHierarchySyncAdapter : ICampaignHierarchySyncAdapter
{
    /// <inheritdoc />
    public string Platform => "Demo";

    /// <inheritdoc />
    public Task<Result<UnifiedCampaignHierarchy>> FetchHierarchyAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        CancellationToken cancellationToken = default)
    {
        var platform = account.Platform;
        var prefix = platform.ToLowerInvariant();

        var cmp1Id = $"{prefix}_demo_cmp_101";
        var cmp2Id = $"{prefix}_demo_cmp_102";
        var cmp3Id = $"{prefix}_demo_cmp_103";

        var campaigns = new List<UnifiedCampaignItem>
        {
            new(
                cmp1Id,
                $"[{platform}] 01. Topo de Funil - Vídeo Awareness & Alcance",
                CampaignStatus.Active,
                "BRAND_AWARENESS",
                120.00m,
                null,
                account.Currency,
                DateTime.UtcNow.AddDays(-30),
                null),
            new(
                cmp2Id,
                $"[{platform}] 02. Meio de Funil - Tráfego Qualificado & Leads",
                CampaignStatus.Active,
                "LEAD_GENERATION",
                250.00m,
                null,
                account.Currency,
                DateTime.UtcNow.AddDays(-20),
                null),
            new(
                cmp3Id,
                $"[{platform}] 03. Fundo de Funil - Remarketing Dinâmico & Vendas",
                CampaignStatus.Active,
                "CONVERSIONS",
                380.00m,
                null,
                account.Currency,
                DateTime.UtcNow.AddDays(-15),
                null)
        };

        var adSets = new List<UnifiedAdSetItem>
        {
            new(
                $"{prefix}_demo_set_201",
                cmp1Id,
                "Interesses Amplos - Empreendedorismo & Marketing",
                AdSetStatus.Active,
                "LOWEST_COST",
                "IMPRESSIONS",
                60.00m,
                null,
                "{\"geo\": [\"BR\"], \"age\": [21, 55]}",
                DateTime.UtcNow.AddDays(-30),
                null),
            new(
                $"{prefix}_demo_set_202",
                cmp1Id,
                "Público Lookalike 2% Envolvimento Instagram",
                AdSetStatus.Active,
                "LOWEST_COST",
                "IMPRESSIONS",
                60.00m,
                null,
                "{\"geo\": [\"BR\"], \"age\": [25, 45]}",
                DateTime.UtcNow.AddDays(-30),
                null),
            new(
                $"{prefix}_demo_set_203",
                cmp2Id,
                "Engajamento 90 Dias & Visitantes do Blog",
                AdSetStatus.Active,
                "TARGET_CPA",
                "LEADS",
                250.00m,
                null,
                "{\"retargeting_window\": 90}",
                DateTime.UtcNow.AddDays(-20),
                null),
            new(
                $"{prefix}_demo_set_204",
                cmp3Id,
                "Carrinho Abandonado 7 Dias (Checkout)",
                AdSetStatus.Active,
                "LOWEST_COST",
                "PURCHASE",
                380.00m,
                null,
                "{\"abandoned_cart\": true, \"days\": 7}",
                DateTime.UtcNow.AddDays(-15),
                null)
        };

        var ads = new List<UnifiedAdItem>
        {
            new(
                $"{prefix}_demo_ad_301",
                $"{prefix}_demo_set_201",
                cmp1Id,
                "Vídeo Institucional - Transformação Digital",
                AdStatus.Active,
                AdCreativeType.Video,
                "Escale suas Vendas Online com AdMetricsPro",
                "Descubra como mais de 500 agências gerenciam milhões em mídia paga com segurança e agilidade.",
                "https://www.admetricspro.com.br/demonstracao",
                "https://cdn.admetricspro.com.br/assets/video_thumb_1.jpg",
                "LEARN_MORE"),
            new(
                $"{prefix}_demo_ad_302",
                $"{prefix}_demo_set_202",
                cmp1Id,
                "Carrossel Benefícios - Gestão Unificada",
                AdStatus.Active,
                AdCreativeType.Carousel,
                "Painel Único para Meta, Google, TikTok e Bing",
                "Elimine planilhas manuais e visualize seu ROI em tempo real.",
                "https://www.admetricspro.com.br/features",
                "https://cdn.admetricspro.com.br/assets/carousel_1.jpg",
                "EXPLORE"),
            new(
                $"{prefix}_demo_ad_303",
                $"{prefix}_demo_set_203",
                cmp2Id,
                "E-book Gratuito: O Guia Definitivo de Tráfego 2026",
                AdStatus.Active,
                AdCreativeType.Image,
                "Baixe o Playbook de Escala de Mídia Paga",
                "Metodologias comprovadas de alocação de verba e automação antibloqueio.",
                "https://www.admetricspro.com.br/ebook-trafego",
                "https://cdn.admetricspro.com.br/assets/ebook_cover.jpg",
                "DOWNLOAD"),
            new(
                $"{prefix}_demo_ad_304",
                $"{prefix}_demo_set_204",
                cmp3Id,
                "Oferta Exclusiva 30% Off no Primeiro Trimestre",
                AdStatus.Active,
                AdCreativeType.Dynamic,
                "Finalize sua Assinatura com Condição Especial",
                "Últimas horas para garantir o plano Pro com suporte prioritário 24/7.",
                "https://www.admetricspro.com.br/checkout-promo",
                "https://cdn.admetricspro.com.br/assets/checkout_banner.jpg",
                "SIGN_UP")
        };

        var hierarchy = new UnifiedCampaignHierarchy(campaigns, adSets, ads);
        return Task.FromResult(Result<UnifiedCampaignHierarchy>.Success(hierarchy));
    }
}
