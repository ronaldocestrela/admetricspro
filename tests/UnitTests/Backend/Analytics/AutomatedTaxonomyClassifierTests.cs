using Analytics.Domain.Taxonomy;
using Analytics.Infrastructure.Taxonomy;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para o motor de taxonomia automatizada ITaxonomyClassifier.
/// </summary>
public sealed class AutomatedTaxonomyClassifierTests
{
    private readonly AutomatedTaxonomyClassifier _classifier = new();

    /// <summary>
    /// Valida que padrões de Topo de Funil / Prospecção são classificados como Top.
    /// </summary>
    /// <param name="campaignName">Nome da campanha sob análise.</param>
    /// <param name="expectedStage">Estágio esperado do funil.</param>
    [Theory]
    [InlineData("[TOF] Campanha Institucional Prospecção", FunnelStage.Top)]
    [InlineData("Campanha Topo de Funil - Vídeo 01", FunnelStage.Top)]
    [InlineData("MetaAds_Prospecting_Broad_Advantage", FunnelStage.Top)]
    [InlineData("Awareness - Reconhecimento de Marca 2026", FunnelStage.Top)]
    public void Classify_WhenTopPatternsMatched_ShouldClassifyAsTop(string campaignName, FunnelStage expectedStage)
    {
        // Act
        var result = _classifier.Classify(campaignName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FunnelStage.Should().Be(expectedStage);
    }

    /// <summary>
    /// Valida que padrões de Meio de Funil / Consideração são classificados como Middle.
    /// </summary>
    /// <param name="campaignName">Nome da campanha sob análise.</param>
    /// <param name="expectedStage">Estágio esperado do funil.</param>
    [Theory]
    [InlineData("[MOF] Campanha Tráfego e Engajamento", FunnelStage.Middle)]
    [InlineData("Campanha Meio de Funil - Vídeo Review", FunnelStage.Middle)]
    [InlineData("GoogleAds_Consideration_Category_Traffic", FunnelStage.Middle)]
    [InlineData("Trafego Qualificado para Blog", FunnelStage.Middle)]
    public void Classify_WhenMiddlePatternsMatched_ShouldClassifyAsMiddle(string campaignName, FunnelStage expectedStage)
    {
        // Act
        var result = _classifier.Classify(campaignName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FunnelStage.Should().Be(expectedStage);
    }

    /// <summary>
    /// Valida que padrões de Fundo de Funil / Conversão / Remarketing são classificados como Bottom.
    /// </summary>
    /// <param name="campaignName">Nome da campanha sob análise.</param>
    /// <param name="expectedStage">Estágio esperado do funil.</param>
    [Theory]
    [InlineData("[BOF] Conversão Direta Checkout", FunnelStage.Bottom)]
    [InlineData("Campanha Fundo de Funil - Oferta Limitada", FunnelStage.Bottom)]
    [InlineData("MetaAds_Remarketing_7D_Catalogo", FunnelStage.Bottom)]
    [InlineData("Google_Search_Vendas_Purchase_RMK", FunnelStage.Bottom)]
    [InlineData("TikTok_Retargeting_ViewContent", FunnelStage.Bottom)]
    public void Classify_WhenBottomPatternsMatched_ShouldClassifyAsBottom(string campaignName, FunnelStage expectedStage)
    {
        // Act
        var result = _classifier.Classify(campaignName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FunnelStage.Should().Be(expectedStage);
    }

    /// <summary>
    /// Valida que padrões de Retenção / LTV / Reativação são classificados como Retention.
    /// </summary>
    /// <param name="campaignName">Nome da campanha sob análise.</param>
    /// <param name="expectedStage">Estágio esperado do funil.</param>
    [Theory]
    [InlineData("[RET] Campanha Reativação Clientes Inativos", FunnelStage.Retention)]
    [InlineData("Retencao_LTV_Assinantes_VIP", FunnelStage.Retention)]
    [InlineData("Winback Churn Prevenção Outubro", FunnelStage.Retention)]
    public void Classify_WhenRetentionPatternsMatched_ShouldClassifyAsRetention(string campaignName, FunnelStage expectedStage)
    {
        // Act
        var result = _classifier.Classify(campaignName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FunnelStage.Should().Be(expectedStage);
    }

    /// <summary>
    /// Valida a identificação correta do tipo de público-alvo na segmentação.
    /// </summary>
    /// <param name="name">Nome contendo indicador de audiência.</param>
    /// <param name="expectedAudience">Tipo de público esperado.</param>
    [Theory]
    [InlineData("Campanha [TOF] - LAL 1% Compradores", AudienceType.Lookalike)]
    [InlineData("Campanha [TOF] - Lookalike Checkout 3%", AudienceType.Lookalike)]
    [InlineData("AdSet Interesses Marketing Digital", AudienceType.Interest)]
    [InlineData("AdSet Publico Aberto Sem Restricoes", AudienceType.Broad)]
    [InlineData("AdSet Broad Targeting Advantage", AudienceType.Broad)]
    [InlineData("Search Institucional Marca AdMetricsPro", AudienceType.SearchBrand)]
    public void Classify_ShouldIdentifyAudienceType(string name, AudienceType expectedAudience)
    {
        // Act
        var result = _classifier.Classify(campaignName: name, adSetName: name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AudienceType.Should().Be(expectedAudience);
    }

    /// <summary>
    /// Valida a identificação do formato de criativo a partir do nome do anúncio.
    /// </summary>
    /// <param name="adName">Nome do criativo/anúncio.</param>
    /// <param name="expectedType">Tipo de formato esperado.</param>
    [Theory]
    [InlineData("Anúncio Vídeo Depoimento Cliente", CreativeType.Video)]
    [InlineData("Criativo Reels 9x16 Bastidores", CreativeType.Video)]
    [InlineData("Criativo Carrossel 5 Beneficios", CreativeType.Carousel)]
    [InlineData("Criativo Imagem Estatica Banner Feed", CreativeType.Image)]
    [InlineData("Campanha Google Search Palavras-Chave", CreativeType.SearchText)]
    public void Classify_ShouldIdentifyCreativeType(string adName, CreativeType expectedType)
    {
        // Act
        var result = _classifier.Classify("Campanha Genérica", adName: adName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreativeType.Should().Be(expectedType);
    }

    /// <summary>
    /// Valida que nomenclaturas sem convenções conhecidas retornam Unclassified de forma amigável e segura.
    /// </summary>
    [Fact]
    public void Classify_WhenNoPatternsMatch_ShouldReturnUnclassifiedGracefullyWithoutException()
    {
        // Arrange
        var name = "Campanha 12345 2026-09";

        // Act
        var result = _classifier.Classify(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FunnelStage.Should().Be(FunnelStage.Unclassified);
        result.Value.AudienceType.Should().Be(AudienceType.Unknown);
        result.Value.CreativeType.Should().Be(CreativeType.Unknown);
    }

    /// <summary>
    /// Valida o processamento e classificação taxonômica em lote de múltiplas campanhas simultaneamente.
    /// </summary>
    [Fact]
    public void ClassifyBatch_ShouldClassifyMultipleCampaignsCorrectly()
    {
        // Arrange
        var inputs = new[]
        {
            new CampaignTaxonomyInput("[TOF] Prospecção Aberta - Vídeo", "Conjunto 01", "Vídeo 01", "ref-1"),
            new CampaignTaxonomyInput("[BOF] Remarketing Checkout - Carrossel", "Conjunto RMK", "Carrossel 01", "ref-2"),
            new CampaignTaxonomyInput("[RET] Fidelização Assinantes", "VIP", "Banner 01", "ref-3")
        };

        // Act
        var result = _classifier.ClassifyBatch(inputs);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        result.Value[0].ReferenceId.Should().Be("ref-1");
        result.Value[0].FunnelStage.Should().Be(FunnelStage.Top);
        result.Value[0].CreativeType.Should().Be(CreativeType.Video);

        result.Value[1].ReferenceId.Should().Be("ref-2");
        result.Value[1].FunnelStage.Should().Be(FunnelStage.Bottom);
        result.Value[1].CreativeType.Should().Be(CreativeType.Carousel);

        result.Value[2].ReferenceId.Should().Be("ref-3");
        result.Value[2].FunnelStage.Should().Be(FunnelStage.Retention);
    }
}
