using Analytics.Application.Creatives.DTOs;
using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;
using WebApp.Services.Creatives;
using Xunit;

namespace UnitTests.Frontend.Components.Creatives;

/// <summary>
/// Testes bUnit para a página de gerenciamento do Creative Hub <see cref="CreativeHubPage"/> (Subfase 5.2.3).
/// </summary>
public sealed class CreativeHubPageTests : BunitTestBase
{
    private readonly ICreativeHubClientService _creativeService = Substitute.For<ICreativeHubClientService>();
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a suíte registrando o serviço cliente nos serviços do bUnit.
    /// </summary>
    public CreativeHubPageTests()
    {
        Services.AddSingleton(_creativeService);
    }

    /// <summary>
    /// Valida que a página renderiza contadores de KPI, alerta visual de substituição e a tabela de criativos.
    /// </summary>
    [Fact]
    public void CreativeHubPage_ShouldRenderKpisAndReplacementAlerts_WhenDataIsLoaded()
    {
        // Arrange
        var adId = Guid.NewGuid();
        var creativeDto = new CreativeFatigueDto(
            adId,
            "Criativo Black Friday",
            "MetaAds",
            "https://cdn.example.com/ad.jpg",
            "Fatigued",
            AnalyzedDays: 7,
            InitialCtr: 3.5m,
            CurrentCtr: 1.2m,
            CtrChangePercentage: -65.7m,
            CtrTrendSlope: -0.32m,
            AverageFrequency: 3.4m,
            CurrentFrequency: 3.8m,
            ReplacementSuggested: true,
            Reason: "Queda severa de CTR e frequência saturada.",
            ActionRecommendation: "Substitua a peça publicitária imediatamente.");

        var overview = new CreativeHubOverviewDto(
            _workspaceId,
            TotalCreatives: 1,
            FatiguedCreativesCount: 1,
            WarningCreativesCount: 0,
            HealthyCreativesCount: 0,
            ReplacementsSuggestedCount: 1,
            Creatives: new List<CreativeFatigueDto> { creativeDto });

        _creativeService.GetOverviewAsync(_workspaceId, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreativeHubOverviewDto>.Success(overview));

        // Act
        var cut = Render<CreativeHubPage>(parameters => parameters
            .Add(p => p.WorkspaceId, _workspaceId));

        // Assert
        cut.WaitForState(() => cut.FindAll("#creative-kpi-cards").Count > 0);

        cut.Find("#kpi-total-creatives").TextContent.Should().Be("1");
        cut.Find("#kpi-fatigued-creatives").TextContent.Should().Be("1");

        // Valida emissão do aviso visual de substituição sugerida na tela do gestor (Subfase 5.2.3)
        cut.FindAll("#section-replacement-alerts").Should().NotBeEmpty();
        cut.Markup.Should().Contain("Aviso de Substituição Sugerida");
        cut.Markup.Should().Contain("Criativo Black Friday");
        cut.Find($"#creative-fatigue-alert-{adId}").Should().NotBeNull();
        cut.Find($"#btn-replace-creative-{adId}").Should().NotBeNull();
    }
}
