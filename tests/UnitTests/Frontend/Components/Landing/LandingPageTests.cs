using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Frontend.Common;
using WebApp.Components.Landing;
using WebApp.Components.Pages;
using Xunit;

namespace UnitTests.Frontend.Components.Landing;

/// <summary>
/// Testes unitários com bUnit para a Landing Page e seus componentes atômicos,
/// validando fidelidade visual, elementos de conversão e estrutura de dados do Stitch.
/// </summary>
public sealed class LandingPageTests : BunitTestBase
{
    /// <summary>
    /// Valida que a Navbar institucional exibe o logotipo oficial do AdMetric Pro,
    /// os links de navegação âncora e os botões de ação para login e teste gratuito.
    /// </summary>
    [Fact]
    public void LandingNavbar_ShouldRenderBrandLinksAndCallToActions()
    {
        // Act
        var cut = Render<LandingNavbar>();

        // Assert
        cut.Find(".landing-brand").TextContent.Should().Contain("AdMetric");
        cut.Find(".landing-brand").TextContent.Should().Contain("Pro");
        cut.Find("a[href='#recursos']").TextContent.Trim().Should().Be("Recursos");
        cut.Find("a[href='#integracoes']").TextContent.Trim().Should().Be("Integrações");
        cut.Find("a[href='#metricas-ia']").TextContent.Trim().Should().Be("Métricas & IA");
        cut.Find("a[href='#depoimentos']").TextContent.Trim().Should().Be("Casos de Sucesso");
        cut.Find("a[href='#precos']").TextContent.Trim().Should().Be("Preços");
        cut.Find(".btn-login").GetAttribute("href").Should().Be("/login");
        cut.Find(".btn-signup").TextContent.Should().Contain("Começar Teste Grátis");
    }

    /// <summary>
    /// Valida que o menu mobile é alternado ao clicar no botão de alternância.
    /// </summary>
    [Fact]
    public void LandingNavbar_WhenMobileMenuButtonClicked_ShouldToggleDropdown()
    {
        // Act & Arrange
        var cut = Render<LandingNavbar>();
        cut.FindAll(".mobile-nav-dropdown").Should().BeEmpty();

        // Clicar para abrir
        var toggleBtn = cut.Find(".btn-mobile-menu");
        toggleBtn.Click();

        // Assert: Dropdown deve estar visível
        cut.Find(".mobile-nav-dropdown").Should().NotBeNull();

        // Clicar para fechar
        toggleBtn.Click();
        cut.FindAll(".mobile-nav-dropdown").Should().BeEmpty();
    }

    /// <summary>
    /// Valida que o Hero renderiza a tag de novo release, headline com ênfase em IA preditiva,
    /// CTAs principais e os selos de confiança.
    /// </summary>
    [Fact]
    public void HeroSection_ShouldRenderHeadlineAnnouncementAndTrustBadges()
    {
        // Act
        var cut = Render<HeroSection>();

        // Assert
        cut.Find(".release-pill").TextContent.Should().Contain("IA Preditiva de ROAS 3.0 disponível");
        cut.Find("h1").TextContent.Should().Contain("Escale seu tráfego pago com inteligência preditiva");
        cut.Find(".cta-trial").TextContent.Should().Contain("Iniciar Teste Grátis de 14 Dias");
        cut.Find(".cta-demo").TextContent.Should().Contain("Agendar Demonstração ao Vivo");
        
        var trustBadges = cut.FindAll(".trust-badge");
        trustBadges.Should().HaveCountGreaterThanOrEqualTo(3);
        cut.Markup.Should().Contain("Sem cartão de crédito");
        cut.Markup.Should().Contain("Integração em 2 minutos");
        cut.Markup.Should().Contain("+R$ 180M gerenciados em ads");
    }

    /// <summary>
    /// Valida que o cockpit analítico em mockup exibe o alerta dinâmico da IA e os 4 KPIs essenciais
    /// com valores fiéis ao Stitch (Investimento, Receita, ROAS 4.30x e CPA).
    /// </summary>
    [Fact]
    public void LiveCockpitMockup_ShouldRenderFloatingAiAlertAndEssentialKpis()
    {
        // Act
        var cut = Render<LiveCockpitMockup>();

        // Assert
        var aiAlert = cut.Find(".ai-floating-pill");
        aiAlert.TextContent.Should().Contain("IA Otimizou em Tempo Real");
        aiAlert.TextContent.Should().Contain("R$ 3.200");

        var kpiCards = cut.FindAll(".kpi-metric-card");
        kpiCards.Should().HaveCount(4);

        // Investimento Total
        cut.Markup.Should().Contain("R$ 428.500");
        // Receita Gerada
        cut.Markup.Should().Contain("R$ 1.842.550");
        // ROAS Médio Consolidado
        cut.Markup.Should().Contain("4.30");
        // Gráfico Vetorial SVG presente
        cut.Find("svg.analytical-chart").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que a fita de integrações apresenta as principais plataformas de anúncios e e-commerce.
    /// </summary>
    [Fact]
    public void IntegrationsSection_ShouldRenderSupportedMediaPlatforms()
    {
        // Act
        var cut = Render<IntegrationsSection>();

        // Assert
        cut.Markup.Should().Contain("Meta Ads");
        cut.Markup.Should().Contain("Google Ads");
        cut.Markup.Should().Contain("TikTok Ads");
        cut.Markup.Should().Contain("Bing Ads");
        cut.Markup.Should().Contain("Shopify");
    }

    /// <summary>
    /// Valida que a grade Bento de benefícios apresenta os 3 pilares de valor do produto.
    /// </summary>
    [Fact]
    public void BentoBenefitsSection_ShouldRenderThreeValuePillars()
    {
        // Act
        var cut = Render<BentoBenefitsSection>();

        // Assert
        var featureCards = cut.FindAll(".bento-card");
        featureCards.Should().HaveCountGreaterThanOrEqualTo(3);
        cut.Markup.Should().Contain("Regras Automatizadas");
        cut.Markup.Should().Contain("Detector de Fadiga");
        cut.Markup.Should().Contain("Atribuição Multi-Toque");
    }

    /// <summary>
    /// Valida a renderização da prova social com depoimentos avaliados em 5 estrelas.
    /// </summary>
    [Fact]
    public void SocialProofSection_ShouldRenderCustomerTestimonialsWithRatings()
    {
        // Act
        var cut = Render<SocialProofSection>();

        // Assert
        var testimonialCards = cut.FindAll(".testimonial-card");
        testimonialCards.Should().HaveCountGreaterThanOrEqualTo(3);
        cut.FindAll(".star-rating").Should().NotBeEmpty();
    }

    /// <summary>
    /// Valida a composição completa da Landing Page em Home.razor.
    /// </summary>
    [Fact]
    public void HomePage_ShouldRenderFullLandingPageStructure()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        cut.FindComponent<LandingNavbar>().Should().NotBeNull();
        cut.FindComponent<HeroSection>().Should().NotBeNull();
        cut.FindComponent<LiveCockpitMockup>().Should().NotBeNull();
        cut.FindComponent<IntegrationsSection>().Should().NotBeNull();
        cut.FindComponent<BentoBenefitsSection>().Should().NotBeNull();
        cut.FindComponent<SocialProofSection>().Should().NotBeNull();
        cut.FindComponent<ConversionBannerSection>().Should().NotBeNull();
        cut.FindComponent<LandingFooter>().Should().NotBeNull();
    }
}
