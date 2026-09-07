using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Onboarding;
using WebApp.Components.Pages;
using WebApp.Models;
using WebApp.Services;
using Xunit;

namespace UnitTests.Frontend.Components.Onboarding;

/// <summary>
/// Testes unitários com bUnit para a página de Onboarding (<see cref="OnboardingPage"/>)
/// e seus componentes atômicos do assistente de provisionamento de novos inquilinos.
/// </summary>
public sealed class OnboardingPageTests : BunitTestBase
{
    private readonly ITenantOnboardingClientService _onboardingClientService = Substitute.For<ITenantOnboardingClientService>();

    public OnboardingPageTests()
    {
        Services.AddSingleton(_onboardingClientService);
    }

    /// <summary>
    /// Valida que a página de onboarding renderiza a estrutura inicial da Etapa 1 (Dados Corporativos)
    /// com a barra de progresso (stepper) e o Live Tenant Preview lateral.
    /// </summary>
    [Fact]
    public void OnboardingPage_ShouldRenderInitialStepAndLivePreview()
    {
        // Act
        var cut = Render<OnboardingPage>();

        // Assert
        cut.Find(".onboarding-stepper").Should().NotBeNull();
        cut.Find(".onboarding-wizard-panel").Should().NotBeNull();
        cut.Find(".live-preview-panel").Should().NotBeNull();
        cut.Find("#company-name").Should().NotBeNull();
        cut.Find("#company-cnpj").Should().NotBeNull();
        cut.Find(".preview-url-box").TextContent.Should().Contain(".admetricspro.com.br");
    }

    /// <summary>
    /// Valida que ao tentar avançar sem preencher a Razão Social ou CNPJ, uma mensagem de validação é exibida.
    /// </summary>
    [Fact]
    public void OnboardingPage_WhenAdvancingWithoutRequiredCompanyData_ShouldShowErrorMessage()
    {
        // Arrange
        var cut = Render<OnboardingPage>();

        // Act - Clica em "Avançar"
        var advanceButton = cut.Find(".wizard-actions .btn-wizard-primary");
        advanceButton.Click();

        // Assert
        var errorPill = cut.Find(".status-pill.error");
        errorPill.Should().NotBeNull();
        errorPill.TextContent.Should().Contain("Razão Social");
    }

    /// <summary>
    /// Valida que o OnboardingStepper renderiza as 4 etapas com os estados ativos corretos.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void OnboardingStepper_ShouldHighlightCurrentActiveStep(int activeStep)
    {
        // Act
        var cut = Render<OnboardingStepper>(parameters => parameters
            .Add(p => p.CurrentStep, activeStep));

        // Assert
        var stepItems = cut.FindAll(".step-item");
        stepItems.Should().HaveCount(4);
        stepItems[activeStep - 1].ClassList.Should().Contain("active");
    }

    /// <summary>
    /// Valida que o LiveTenantPreviewCard reage em tempo real aos dados preenchidos no formulário.
    /// </summary>
    [Fact]
    public void LiveTenantPreviewCard_ShouldDisplayTenantBrandAndSubdomain()
    {
        // Arrange
        var model = new TenantOnboardingFormModel
        {
            CompanyName = "Vanguarda Tech",
            Subdomain = "vanguarda",
            PrimaryColor = "#4f46e5"
        };

        // Act
        var cut = Render<LiveTenantPreviewCard>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        cut.Find(".preview-url-box").TextContent.Should().Contain("vanguarda.admetricspro.com.br");
        cut.Find(".mockup-brand").TextContent.Should().Contain("Vanguarda Tech");
        cut.Find(".telemetry-terminal").TextContent.Should().Contain("Tenant_vanguarda");
    }
}
