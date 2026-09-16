using System.Text.RegularExpressions;
using Analytics.Domain.Taxonomy;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Infrastructure.Taxonomy;

/// <summary>
/// Motor determinístico de alto desempenho para classificação taxonômica de campanhas, conjuntos de anúncios e criativos.
/// </summary>
public sealed class AutomatedTaxonomyClassifier : ITaxonomyClassifier
{
    private static readonly Regex DelimitersRegex = new(
        @"[_\-\|/\[\]\(\)\:\.\,\+]+",
        RegexOptions.Compiled);

    // Regex de Estágios de Funil compiladas
    private static readonly Regex TopFunnelRegex = new(
        @"(?:\[TOF\]|\b(TOF|Topo|Prospec[cç][aã]o|Prospecting|Awareness|Reconhecimento|Alcance)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MiddleFunnelRegex = new(
        @"(?:\[MOF\]|\b(MOF|Meio|Engajamento|Engagement|Considera[cç][aã]o|Consideration|Tr[aá]fego|Traffic)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BottomFunnelRegex = new(
        @"(?:\[BOF\]|\b(BOF|Fundo|Remarketing|Retargeting|RMK|Convers[aã]o|Conversion|Vendas|Purchase|Checkout)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RetentionFunnelRegex = new(
        @"(?:\[RET\]|\b(RET|Reten[cç][aã]o|Reativa[cç][aã]o|LTV|Churn|Winback|Fideliza[cç][aã]o)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex de Tipo de Público compiladas
    private static readonly Regex LookalikeRegex = new(
        @"\b(LAL|Lookalike|Semelhante)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BroadRegex = new(
        @"\b(Aberto|Broad|Amplo)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InterestRegex = new(
        @"\b(Interesse|Interesses|Interest)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CustomAudienceRegex = new(
        @"\b(CustomAudience|Lista|Visitantes|Website)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SearchBrandRegex = new(
        @"\b(Institucional|Brand|Marca)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SearchNonBrandRegex = new(
        @"\b(Non-?Brand|Gen[eé]rico)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex de Formato de Criativo compiladas
    private static readonly Regex VideoRegex = new(
        @"\b(V[ií]deo|Reels|Shorts|TikTok|YouTube|Motion)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CarouselRegex = new(
        @"\b(Carrossel|Carousel|DPA)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ImageRegex = new(
        @"\b(Imagem|Est[aá]tica|Banner|Feed|Display)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SearchTextRegex = new(
        @"\b(Search|Pesquisa|Palavras-?Chave)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DynamicRegex = new(
        @"\b(Din[aâ]mico|Advantage\+|PMax)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <inheritdoc />
    public Result<TaxonomyClassificationResult> Classify(
        string campaignName,
        string? adSetName = null,
        string? adName = null,
        string? referenceId = null)
    {
        var campaign = campaignName ?? string.Empty;
        var combinedText = $"{campaign} {adSetName ?? string.Empty} {adName ?? string.Empty}";

        // Normaliza delimitadores comuns de mídia paga (_ - | / . : [ ]) para espaço para garantir limites de palavra \b
        var normalizedText = DelimitersRegex.Replace(combinedText, " ");

        var stage = ClassifyFunnelStage(combinedText, normalizedText);
        var audience = ClassifyAudienceType(combinedText, normalizedText);
        var creative = ClassifyCreativeType(combinedText, normalizedText);

        var tags = new List<string>();
        var detailedTags = new List<TaxonomyTag>();

        if (stage != FunnelStage.Unclassified)
        {
            var stageName = stage.ToString();
            tags.Add(stageName);
            detailedTags.Add(new TaxonomyTag("FunnelStage", stageName, stageName));
        }

        if (audience != AudienceType.Unknown)
        {
            var audienceName = audience.ToString();
            tags.Add(audienceName);
            detailedTags.Add(new TaxonomyTag("AudienceType", audienceName, audienceName));
        }

        if (creative != CreativeType.Unknown)
        {
            var creativeName = creative.ToString();
            tags.Add(creativeName);
            detailedTags.Add(new TaxonomyTag("CreativeType", creativeName, creativeName));
        }

        var result = new TaxonomyClassificationResult
        {
            ReferenceId = referenceId,
            CampaignName = campaign,
            AdSetName = adSetName,
            AdName = adName,
            FunnelStage = stage,
            AudienceType = audience,
            CreativeType = creative,
            Tags = tags.AsReadOnly(),
            DetailedTags = detailedTags.AsReadOnly()
        };

        return Result<TaxonomyClassificationResult>.Success(result);
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<TaxonomyClassificationResult>> ClassifyBatch(
        IEnumerable<CampaignTaxonomyInput> items)
    {
        if (items is null)
        {
            return Result<IReadOnlyList<TaxonomyClassificationResult>>.Failure(
                Error.Validation("TaxonomyClassifier.NullItems", "A lista de itens para classificação taxonômica não pode ser nula."));
        }

        var results = new List<TaxonomyClassificationResult>();

        foreach (var item in items)
        {
            var classification = Classify(item.CampaignName, item.AdSetName, item.AdName, item.ReferenceId);
            if (classification.IsSuccess)
            {
                results.Add(classification.Value);
            }
        }

        return Result<IReadOnlyList<TaxonomyClassificationResult>>.Success(results.AsReadOnly());
    }

    private static FunnelStage ClassifyFunnelStage(string rawText, string normalizedText)
    {
        // Prioridade de avaliação: Fundo -> Retenção -> Meio -> Topo
        if (BottomFunnelRegex.IsMatch(rawText) || BottomFunnelRegex.IsMatch(normalizedText)) return FunnelStage.Bottom;
        if (RetentionFunnelRegex.IsMatch(rawText) || RetentionFunnelRegex.IsMatch(normalizedText)) return FunnelStage.Retention;
        if (MiddleFunnelRegex.IsMatch(rawText) || MiddleFunnelRegex.IsMatch(normalizedText)) return FunnelStage.Middle;
        if (TopFunnelRegex.IsMatch(rawText) || TopFunnelRegex.IsMatch(normalizedText)) return FunnelStage.Top;

        return FunnelStage.Unclassified;
    }

    private static AudienceType ClassifyAudienceType(string rawText, string normalizedText)
    {
        if (LookalikeRegex.IsMatch(rawText) || LookalikeRegex.IsMatch(normalizedText)) return AudienceType.Lookalike;
        if (CustomAudienceRegex.IsMatch(rawText) || CustomAudienceRegex.IsMatch(normalizedText)) return AudienceType.CustomAudience;
        if (InterestRegex.IsMatch(rawText) || InterestRegex.IsMatch(normalizedText)) return AudienceType.Interest;
        if (BroadRegex.IsMatch(rawText) || BroadRegex.IsMatch(normalizedText)) return AudienceType.Broad;
        if (SearchBrandRegex.IsMatch(rawText) || SearchBrandRegex.IsMatch(normalizedText)) return AudienceType.SearchBrand;
        if (SearchNonBrandRegex.IsMatch(rawText) || SearchNonBrandRegex.IsMatch(normalizedText)) return AudienceType.SearchNonBrand;

        return AudienceType.Unknown;
    }

    private static CreativeType ClassifyCreativeType(string rawText, string normalizedText)
    {
        if (CarouselRegex.IsMatch(rawText) || CarouselRegex.IsMatch(normalizedText)) return CreativeType.Carousel;
        if (VideoRegex.IsMatch(rawText) || VideoRegex.IsMatch(normalizedText)) return CreativeType.Video;
        if (ImageRegex.IsMatch(rawText) || ImageRegex.IsMatch(normalizedText)) return CreativeType.Image;
        if (SearchTextRegex.IsMatch(rawText) || SearchTextRegex.IsMatch(normalizedText)) return CreativeType.SearchText;
        if (DynamicRegex.IsMatch(rawText) || DynamicRegex.IsMatch(normalizedText)) return CreativeType.Dynamic;

        return CreativeType.Unknown;
    }
}
