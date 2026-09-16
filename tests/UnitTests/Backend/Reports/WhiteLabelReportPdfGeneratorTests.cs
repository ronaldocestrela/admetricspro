using System.Text;
using Analytics.Domain.Reports;
using Analytics.Infrastructure.Reports;
using BuildingBlocks.Domain.Reports;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes unitários para o gerador de documentos PDF executivos com White-Label estrito (<see cref="WhiteLabelReportPdfGenerator"/>).
/// </summary>
public sealed class WhiteLabelReportPdfGeneratorTests
{
    private readonly WhiteLabelReportPdfGenerator _generator = new();

    /// <summary>
    /// Valida que a geração de PDF produz cabeçalho mágico padrão, aplica dados da agência parceira
    /// e garante que nenhuma referência ao AdMetricsPro esteja presente no documento.
    /// </summary>
    [Fact]
    public async Task GeneratePdfAsync_ShouldProduceValidPdfHeaderAndStrictWhiteLabel()
    {
        // Arrange
        var branding = new ReportBrandingSnapshot(
            PrimaryColor: "#10B981",
            SecondaryColor: "#1E293B",
            LightLogoUrl: "https://cdn.agenciadigital.com.br/logo.png",
            DarkLogoUrl: null,
            FaviconUrl: null,
            AgencyName: "Agência Impulso Digital",
            SupportEmail: "contato@impulsodigital.com.br",
            SupportPhone: "+55 11 98888-7777",
            CustomDomain: "relatorios.impulsodigital.com.br");

        var model = new ReportRenderModel
        {
            ReportTitle = "Relatório de Resultados - E-commerce de Calçados",
            WorkspaceName = "Calçados Elegance",
            DateRangeStart = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            DateRangeEnd = new DateTime(2026, 9, 15, 23, 59, 59, DateTimeKind.Utc),
            Branding = branding,
            KpiSummary = new ReportKpiSummary
            {
                TotalSpend = 15000.50m,
                TotalRevenue = 64500.00m,
                BlendedRoas = 4.30m,
                BlendedCpa = 32.50m,
                TotalConversions = 461,
                TotalClicks = 8250,
                TotalImpressions = 210000
            },
            ChannelBreakdown = new List<ReportChannelMetric>
            {
                new() { Platform = "MetaAds", Spend = 8000m, Revenue = 36000m, Roas = 4.5m, Conversions = 260, SharePercentage = 53.3m },
                new() { Platform = "GoogleAds", Spend = 5000m, Revenue = 22000m, Roas = 4.4m, Conversions = 150, SharePercentage = 33.3m },
                new() { Platform = "TikTokAds", Spend = 2000.5m, Revenue = 6500m, Roas = 3.25m, Conversions = 51, SharePercentage = 13.4m }
            },
            TopCreatives = new List<ReportTopCreative>
            {
                new() { AdName = "Vídeo Tênis Esportivo Pro", Platform = "MetaAds", Spend = 2500m, Ctr = 2.85m, Roas = 5.2m, FatigueStatus = "Saudável" },
                new() { AdName = "Carrossel Coleção Primavera", Platform = "TikTokAds", Spend = 1200m, Ctr = 1.95m, Roas = 3.8m, FatigueStatus = "Fadiga Leve" }
            },
            CopilotInsights = new List<string>
            {
                "Meta Ads apresentou o maior ROAS blended (4.50x), sendo responsável por 53% das conversões totais.",
                "O criativo 'Vídeo Tênis Esportivo Pro' obteve CTR 45% acima da média da conta."
            },
            CustomNotes = "Parabéns equipe! Excelente eficiência no meio do mês. Recomendamos elevar verba no conjunto de alta escala.",
            GeneratedAt = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc),
            ShareUrl = "https://relatorios.impulsodigital.com.br/r/a1b2c3d4e5f6"
        };

        // Act
        var result = await _generator.GeneratePdfAsync(model);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pdfBytes = result.Value;
        pdfBytes.Should().NotBeNullOrEmpty();
        pdfBytes.Length.Should().BeGreaterThan(100);

        // Converte bytes para string para inspecionar comandos PDF
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        // 1. Cabeçalho mágico do formato PDF
        pdfText.Should().StartWith("%PDF-1.");

        // 2. Validação White-Label estrita: NÃO pode conter nenhuma menção ao AdMetricsPro
        pdfText.Should().NotContain("AdMetricsPro", "O relatório PDF deve ser estritamente White-Label sem citar a plataforma AdMetricsPro.");

        // 3. Deve conter os dados da agência parceira
        pdfText.Should().Contain("Agencia Impulso Digital");
        pdfText.Should().Contain("Calcados Elegance");
    }
}
