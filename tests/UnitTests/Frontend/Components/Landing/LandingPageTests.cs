using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Landing;
using WebApp.Components.Pages;

namespace UnitTests.Frontend.Components.Landing;

/// <summary>Verifica a estrutura e as interações da landing page adaptada da referência Sendium.</summary>
public sealed class LandingPageTests : BunitTestBase
{
    /// <summary>Configura os módulos visuais sem exigir navegador nos testes de componentes.</summary>
    public LandingPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Exige as dez seções, âncoras válidas e conteúdo específico do produto.</summary>
    [Fact]
    public void HomePage_ShouldPreserveReferenceStructureAndUseSupportedProductClaims()
    {
        var cut = Render<Home>();
        cut.FindAll("main section").Should().HaveCount(10);
        cut.Find("h1").TextContent.Should().Contain("verba");
        cut.Markup.Should().Contain("Detector de Fadiga de Criativos");
        cut.Markup.Should().Contain("Atribuição Multi-Toque");
        cut.Markup.Should().Contain("MER Unificado");
        cut.Markup.Should().NotContain("Telegram").And.NotContain("Sendium");
        cut.FindAll(".hero-carousel-image").Should().HaveCount(3);
        cut.FindAll(".timeline-node").Should().HaveCount(6);
        cut.FindAll("[role='tab']").Should().HaveCount(4);
        cut.FindAll("[role='tabpanel'] .p-space-md.rounded-lg").Should().HaveCount(24);
        cut.FindAll(".faq-item").Should().HaveCount(6);
        cut.FindAll(".pricing-card").Should().HaveCount(3);
        foreach (var link in cut.FindAll("a[href^='#']"))
        {
            var href = link.GetAttribute("href")!;
            cut.FindAll(href).Should().ContainSingle($"a âncora {href} deve existir");
        }
        cut.FindAll("a[href='/onboarding']").Should().NotBeEmpty();
        cut.FindAll("a[href='/login']").Should().NotBeEmpty();
        cut.Find("[data-scroll-reveal-root]").HasAttribute("data-reveal-pending").Should().BeFalse(
            "o conteúdo pré-renderizado deve ser legível antes da inicialização dos efeitos");
    }

    /// <summary>Preserva a ordem da arquitetura de informação da referência.</summary>
    [Fact]
    public void HomePage_ShouldKeepSectionOrder()
    {
        var cut = Render<Home>();
        cut.FindAll("main section h2, main section h1").Select(x => x.TextContent.Trim()).Should().Equal(
            "Sua verba merece decisões melhores. Seu tráfego também.",
            "Você compra mídia. Mas consegue enxergar o que ela entrega?",
            "Da primeira conexão à próxima decisão de investimento.",
            "Por que escolher o AdMetricsPro?",
            "Para quem precisa fazer a verba trabalhar melhor.",
            "Cresça com controle sobre dados, acessos e decisões.",
            "Escolha a estrutura para o seu próximo estágio.",
            "Perguntas Frequentes",
            "Pare de juntar relatórios. Comece a enxergar decisões.");
    }

    /// <summary>Fecha o menu móvel ao escolher um destino e atualiza o estado acessível.</summary>
    [Fact]
    public void Navbar_ShouldToggleMobileMenuAndCloseOnNavigation()
    {
        var cut = Render<LandingNavbar>();
        cut.Find("#mobile-menu").HasAttribute("hidden").Should().BeTrue();
        cut.Find("#mobile-menu-toggle").Click();
        cut.Find("#mobile-menu-toggle").GetAttribute("aria-expanded").Should().Be("true");
        cut.Find("#mobile-menu").HasAttribute("hidden").Should().BeFalse();
        cut.Find("#mobile-menu a[href='#recursos']").Click();
        cut.Find("#mobile-menu").HasAttribute("hidden").Should().BeTrue();
        cut.Find("#mobile-menu-toggle").GetAttribute("aria-expanded").Should().Be("false");
    }

    /// <summary>Mantém a indicação da seção ativa e o fechamento por interop do navegador.</summary>
    [Fact]
    public async Task Navbar_ShouldRespondToBrowserNavigationState()
    {
        var cut = Render<LandingNavbar>();
        await cut.InvokeAsync(() => cut.Instance.SetActiveSection("recursos"));
        cut.Find("nav[aria-label='Navegação principal'] a[href='#recursos']")
            .GetAttribute("aria-current").Should().Be("location");
        cut.Find("#mobile-menu-toggle").Click();
        await cut.InvokeAsync(cut.Instance.CloseMenu);
        cut.Find("#mobile-menu").HasAttribute("hidden").Should().BeTrue();
    }

    /// <summary>Preserva a seleção manual, a navegação circular e o avanço pelo temporizador.</summary>
    [Fact]
    public async Task Hero_ShouldNavigateDemonstrationsManuallyAndThroughInterop()
    {
        var cut = Render<HeroSection>();
        cut.Find(".hero-carousel-image.is-active").GetAttribute("src").Should().EndWith("overview.svg");
        cut.Find("button[aria-label='Imagem anterior']").Click();
        cut.Find(".hero-carousel-image.is-active").GetAttribute("src").Should().EndWith("pacing.svg");
        cut.Find("button[aria-label='Próxima imagem']").Click();
        cut.Find(".hero-carousel-image.is-active").GetAttribute("src").Should().EndWith("overview.svg");
        cut.Find("button[aria-label='Imagem 2']").Click();
        cut.Find(".hero-carousel-image.is-active").GetAttribute("src").Should().EndWith("creatives.svg");
        await cut.InvokeAsync(cut.Instance.AdvanceSlide);
        cut.Find(".hero-carousel-image.is-active").GetAttribute("src").Should().EndWith("pacing.svg");
        cut.Find(".hero-carousel-controls span").TextContent.Should().Contain("dados demonstrativos");
    }

    /// <summary>Troca o painel e mantém os vínculos acessíveis entre abas e conteúdo.</summary>
    [Fact]
    public void Resources_ShouldSelectMatchingPanel()
    {
        var cut = Render<ResourcesSection>();
        cut.Find("#resource-tab-3").Click();
        cut.Find("#resource-tab-3").GetAttribute("aria-selected").Should().Be("true");
        cut.Find("#resource-tab-1").GetAttribute("aria-selected").Should().Be("false");
        cut.Find("#resource-panel-3").ClassList.Should().NotContain("hidden");
        cut.Find("#resource-panel-1").ClassList.Should().Contain("hidden");
        cut.Find("#resource-panel-3").TextContent.Should().Contain("Pacing de orçamento");
    }

    /// <summary>Abre apenas uma resposta por vez e permite recolher a pergunta ativa.</summary>
    [Fact]
    public void Faq_ShouldKeepOneAnswerOpenAndAllowClosing()
    {
        var cut = Render<FaqSection>();
        cut.Find("#faq-question-1").GetAttribute("aria-expanded").Should().Be("true");
        cut.Find("#faq-question-3").Click();
        cut.Find("#faq-answer-1").HasAttribute("hidden").Should().BeTrue();
        cut.Find("#faq-answer-3").HasAttribute("hidden").Should().BeFalse();
        cut.Find("#faq-answer-3").TextContent.Should().Contain("jornadas de conversão fornecidas");
        cut.Find("#faq-question-3").Click();
        cut.FindAll(".faq-content:not([hidden])").Should().BeEmpty();
    }

    /// <summary>Evita oferta sem destino e identifica os valores comerciais como referências.</summary>
    [Fact]
    public void Pricing_ShouldUseSignupAndExplainCommercialConditions()
    {
        var cut = Render<PricingSection>();
        cut.FindAll(".pricing-card a").Should().HaveCount(3)
            .And.OnlyContain(a => a.GetAttribute("href") == "/onboarding");
        cut.Find("p").TextContent.Should().Contain("14 dias gratuitos").And.Contain("cadastro");
        cut.FindAll("h3").Select(x => x.TextContent.Trim()).Should().Equal("Starter", "Pro", "Enterprise");
    }
}
