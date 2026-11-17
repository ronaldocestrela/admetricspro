using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Traction;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes unitários do detector de tração orgânica (Subfase 6.1).
/// </summary>
public sealed class OrganicTractionDetectorTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
    private static readonly DateTime AsOfUtc = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Menos de três posts com impressões na rede deve falhar com dados insuficientes.
    /// </summary>
    [Fact]
    public void Detect_ComMenosDeTresPostsComImpressoes_DeveRetornarInsufficientData()
    {
        var series = new[]
        {
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 1),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 2),
        };

        var result = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OrganicTractionInsufficientData.Code);
    }

    /// <summary>
    /// Redes são independentes: Instagram com três posts pode gerar hits mesmo com TikTok abaixo do mínimo.
    /// </summary>
    [Fact]
    public void Detect_ComRedesIndependentes_InstagramComTresPostsPodeGerarHit()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var series = new[]
        {
            Sample(OrganicPlatform.TikTok, impressions: 5000, likes: 50, publishedDaysAgo: 1),
            Sample(OrganicPlatform.Instagram, id1, impressions: 1000, likes: 20, publishedDaysAgo: 1),
            Sample(OrganicPlatform.Instagram, id2, impressions: 1000, likes: 20, publishedDaysAgo: 2),
            Sample(OrganicPlatform.Instagram, id3, impressions: 1000, likes: 60, publishedDaysAgo: 3),
        };

        var result = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Hits.Should().ContainSingle(h => h.OrganicPostId == id3);
        result.Value.Hits.Single().EngagementMultiple.Should().Be(3.0m);
    }

    /// <summary>
    /// Nenhuma rede com janela mínima deve falhar com dados insuficientes.
    /// </summary>
    [Fact]
    public void Detect_QuandoNenhumaRedeFechaJanela_DeveRetornarInsufficientData()
    {
        var series = new[]
        {
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 1),
            Sample(OrganicPlatform.TikTok, impressions: 1000, likes: 20, publishedDaysAgo: 2),
        };

        var result = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OrganicTractionInsufficientData.Code);
    }

    /// <summary>
    /// Posts de outro workspace não entram na mediana nem na contagem.
    /// </summary>
    [Fact]
    public void Detect_IgnoraPostsDeOutroWorkspace()
    {
        var otherWorkspace = Guid.NewGuid();
        var series = new[]
        {
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 1),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 2),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 3, workspaceId: otherWorkspace),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 4, workspaceId: otherWorkspace),
        };

        var result = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OrganicTractionInsufficientData.Code);
    }

    /// <summary>
    /// Impressões zero ficam fora da janela de baseline.
    /// </summary>
    [Fact]
    public void Detect_ImpressoesZeroNaoEntramNaJanela()
    {
        var series = new[]
        {
            Sample(OrganicPlatform.Instagram, impressions: 0, likes: 100, publishedDaysAgo: 1),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 2),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 3),
        };

        var result = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OrganicTractionInsufficientData.Code);
    }

    /// <summary>
    /// Post fora da janela de 28 dias não entra na mediana.
    /// </summary>
    [Fact]
    public void Detect_PostForaDeVinteOitoDiasNaoEntraNaJanela()
    {
        var series = new[]
        {
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 1),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 2),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 29),
        };

        var result = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OrganicTractionInsufficientData.Code);
    }

    /// <summary>
    /// Mediana com quantidade par usa média dos dois centrais; hit exige 2× mediana e impressões mínimas.
    /// </summary>
    [Fact]
    public void Detect_MedianaPar_EngajamentoNoLimiarContaComoHit()
    {
        var hitId = Guid.NewGuid();
        var series = new[]
        {
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 1),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 2),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 40, publishedDaysAgo: 3),
            Sample(OrganicPlatform.Instagram, hitId, impressions: 1000, likes: 60, publishedDaysAgo: 4),
        };

        var result = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Hits.Should().ContainSingle(h => h.OrganicPostId == hitId);
        result.Value.Hits.Single().BaselineMedianEngagement.Should().Be(0.03m);
        result.Value.Hits.Single().EngagementRate.Should().Be(0.06m);
    }

    /// <summary>
    /// Impressões abaixo do mínimo não geram hit, mas entram na mediana.
    /// </summary>
    [Fact]
    public void Detect_ImpressoesAbaixoDoMinimo_NaoGeraHitMasEntraNaMediana()
    {
        var lowImpressionsId = Guid.NewGuid();
        var series = new[]
        {
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 1),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 2),
            Sample(OrganicPlatform.Instagram, lowImpressionsId, impressions: 999, likes: 999, publishedDaysAgo: 3),
        };

        var result = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Hits.Should().BeEmpty();
    }

    /// <summary>
    /// Post com sugestão aberta não entra em hits e aparece em ignorados com BoostAlreadyRequested.
    /// </summary>
    [Fact]
    public void Detect_ComSugestaoAberta_NaoDisparaOutroHit()
    {
        var openSuggestionId = Guid.NewGuid();
        var otherHitId = Guid.NewGuid();
        var series = new[]
        {
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 1),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 2),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 3),
            Sample(OrganicPlatform.Instagram, openSuggestionId, impressions: 1000, likes: 60, publishedDaysAgo: 4, hasOpenSuggestion: true),
            Sample(OrganicPlatform.Instagram, otherHitId, impressions: 1000, likes: 60, publishedDaysAgo: 5),
        };

        var result = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Hits.Should().ContainSingle(h => h.OrganicPostId == otherHitId);
        result.Value.SkippedDueToOpenSuggestion.Should().ContainSingle(s =>
            s.OrganicPostId == openSuggestionId &&
            s.Error.Code == SocialErrors.BoostAlreadyRequested.Code);
    }

    /// <summary>
    /// Detector é função pura: mesma série e mesmo instante produzem o mesmo relatório.
    /// </summary>
    [Fact]
    public void Detect_MesmaSerieEMesmoAsOfUtc_ProduzMesmoRelatorio()
    {
        var series = new[]
        {
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 1),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 20, publishedDaysAgo: 2),
            Sample(OrganicPlatform.Instagram, impressions: 1000, likes: 60, publishedDaysAgo: 3),
        };

        var first = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);
        var second = OrganicTractionDetector.Detect(WorkspaceId, series, AsOfUtc);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value!.Hits.Should().BeEquivalentTo(second.Value!.Hits);
        first.Value.SkippedDueToOpenSuggestion.Should().BeEquivalentTo(second.Value.SkippedDueToOpenSuggestion);
    }

    private static OrganicPostInsightSample Sample(
        OrganicPlatform platform,
        Guid? postId = null,
        long impressions = 1000,
        long likes = 20,
        int publishedDaysAgo = 1,
        Guid? workspaceId = null,
        bool hasOpenSuggestion = false,
        long comments = 0,
        long shares = 0,
        long saves = 0) =>
        new(
            workspaceId ?? WorkspaceId,
            postId ?? Guid.NewGuid(),
            platform,
            AsOfUtc.AddDays(-publishedDaysAgo),
            impressions,
            likes,
            comments,
            shares,
            saves,
            hasOpenSuggestion);
}
