using Analytics.Application.Reports.DTOs;
using Analytics.Domain.Reports;
using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages.Reports;
using WebApp.Services.Reports;
using Xunit;

namespace UnitTests.Frontend.Components.Reports;

/// <summary>
/// Testes de componente bUnit para a página pública de visualização de relatório interativo <see cref="SharedReportPage"/> (Subfase 5.4.3).
/// </summary>
public sealed class SharedReportPageTests : BunitTestBase
{
    private readonly IPublicReportClientService _publicReportClientService = Substitute.For<IPublicReportClientService>();

    /// <summary>
    /// Inicializa a suíte registrando o serviço público de relatórios nos serviços do bUnit.
    /// </summary>
    public SharedReportPageTests()
    {
        Services.AddSingleton(_publicReportClientService);
    }

    /// <summary>
    /// Valida que a página pública carrega e renderiza métricas de alto impacto, logotipo White-Label e rodapé institucional sem marcas da plataforma.
    /// </summary>
    [Fact]
    public void SharedReportPage_ShouldRenderWhiteLabelReport_WhenTokenIsValid()
    {
        // Arrange
        const string token = "token-alpha-valid-123456";
        var branding = new ReportBrandingDto(
            PrimaryColor: "#4F46E5",
            SecondaryColor: "#1E1B4B",
            LightLogoUrl: "https://cdn.example.com/elite-logo.png",
            DarkLogoUrl: null,
            FaviconUrl: null,
            AgencyName: "Agência Elite Growth",
            SupportEmail: "contato@elitegrowth.com.br",
            SupportPhone: "+5511999998888",
            CustomDomain: "relatorios.elitegrowth.com.br");

        var kpis = new ReportKpiSummary
        {
            TotalSpend = 15420.50m,
            TotalRevenue = 61682.00m,
            BlendedRoas = 4.0m,
            BlendedCpa = 16.76m,
            TotalConversions = 920,
            TotalClicks = 18000,
            TotalImpressions = 450000
        };

        var channels = new List<ReportChannelMetric>
        {
            new() { Platform = "Meta Ads", Spend = 10200m, Revenue = 42840m, Roas = 4.2m, Conversions = 620, SharePercentage = 66.1m },
            new() { Platform = "Google Ads", Spend = 5220.50m, Revenue = 18842m, Roas = 3.6m, Conversions = 300, SharePercentage = 33.9m }
        };

        var creatives = new List<ReportTopCreative>
        {
            new() { AdName = "Criativo Vídeo VSL 01", Platform = "Meta Ads", Spend = 3200m, Ctr = 4.2m, Roas = 4.5m, FatigueStatus = "Saudável" }
        };

        var sharedReport = new PublicSharedReportDto(
            ReportTitle: "Relatório de Performance - Setembro 2026",
            WorkspaceName: "Cliente Alpha",
            DateRangeStart: DateTime.UtcNow.AddDays(-30),
            DateRangeEnd: DateTime.UtcNow,
            Branding: branding,
            KpiSummary: kpis,
            ChannelBreakdown: channels,
            TopCreatives: creatives,
            CopilotInsights: new List<string> { "Desempenho consistente em Meta Ads e Google Ads." },
            CustomNotes: "Estratégia com alto retorno no funil de vendas.",
            GeneratedAt: DateTime.UtcNow,
            IsExpired: false,
            ShareToken: token);

        _publicReportClientService.GetSharedReportAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result<PublicSharedReportDto>.Success(sharedReport));

        // Act
        var cut = Render<SharedReportPage>(parameters => parameters
            .Add(p => p.Token, token));

        // Assert
        cut.WaitForState(() => cut.FindAll("#shared-report-content").Count > 0);

        cut.Find("#header-agency-name").TextContent.Should().Contain("Agência Elite Growth");
        cut.Find("#header-report-title").TextContent.Should().Be("Relatório de Performance - Setembro 2026");
        cut.Find("#kpi-spend-value").TextContent.Should().Contain("15").And.Contain("420");
        cut.Find("#kpi-roas-value").TextContent.Should().Contain("4").And.Contain("x");
        cut.Find("#kpi-conversions-value").TextContent.Should().Contain("920");

        // Valida rodapé institucional e ausência absoluta da marca AdMetricsPro
        cut.Find("#footer-agency-info").TextContent.Should().Contain("contato@elitegrowth.com.br");
        cut.Markup.Should().NotContain("AdMetricsPro");
    }

    /// <summary>
    /// Valida que ao acessar com token expirado, a tela apresenta o estado visual de link expirado.
    /// </summary>
    [Fact]
    public void SharedReportPage_ShouldRenderExpiredState_WhenTokenIsExpired()
    {
        // Arrange
        const string token = "token-expired-999";
        var branding = new ReportBrandingDto(
            PrimaryColor: "#4F46E5",
            SecondaryColor: "#1E1B4B",
            LightLogoUrl: null,
            DarkLogoUrl: null,
            FaviconUrl: null,
            AgencyName: "Agência Elite Growth",
            SupportEmail: null,
            SupportPhone: null,
            CustomDomain: null);

        var sharedReport = new PublicSharedReportDto(
            ReportTitle: "Relatório Antigo Expirado",
            WorkspaceName: "Cliente Alpha",
            DateRangeStart: DateTime.UtcNow.AddDays(-60),
            DateRangeEnd: DateTime.UtcNow.AddDays(-30),
            Branding: branding,
            KpiSummary: new ReportKpiSummary(),
            ChannelBreakdown: new List<ReportChannelMetric>(),
            TopCreatives: new List<ReportTopCreative>(),
            CopilotInsights: new List<string>(),
            CustomNotes: null,
            GeneratedAt: DateTime.UtcNow.AddDays(-30),
            IsExpired: true,
            ShareToken: token);

        _publicReportClientService.GetSharedReportAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result<PublicSharedReportDto>.Success(sharedReport));

        // Act
        var cut = Render<SharedReportPage>(parameters => parameters
            .Add(p => p.Token, token));

        // Assert
        cut.WaitForState(() => cut.FindAll("#shared-report-expired").Count > 0);
        cut.Markup.Should().Contain("Link Expirado");
        cut.Markup.Should().Contain("Este relatório interativo atingiu sua data limite de validade");
    }
}
