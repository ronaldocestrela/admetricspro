using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Onboarding;
using WebApp.Components.Pages;
using WebApp.Models;
using WebApp.Services;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Components.Onboarding;

/// <summary>
/// Testes unitários com bUnit para a página de Onboarding (<see cref="OnboardingPage"/>)
/// e seus componentes atômicos do assistente de provisionamento de novos inquilinos.
/// </summary>
public sealed class OnboardingPageTests : BunitTestBase
{
    private readonly ITenantOnboardingClientService _onboardingClientService = Substitute.For<ITenantOnboardingClientService>();
    private readonly ITenantAuthClientService _authClientService = Substitute.For<ITenantAuthClientService>();
    private readonly TenantStateProvider _tenantStateProvider = new();
    private readonly TenantSessionStateProvider _sessionProvider;

    public OnboardingPageTests()
    {
        _sessionProvider = new TenantSessionStateProvider(_tenantStateProvider);

        _onboardingClientService.CheckTaxDocumentAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Master.Application.Tenants.Queries.CheckTaxDocumentAvailability.TaxDocumentAvailabilityResponse>.Success(
                new Master.Application.Tenants.Queries.CheckTaxDocumentAvailability.TaxDocumentAvailabilityResponse(
                    "52998224725", "529.982.247-25", true, true, "CPF")));

        _onboardingClientService.CheckSubdomainAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Master.Application.Tenants.Queries.CheckSubdomainAvailability.SubdomainAvailabilityResponse>.Success(
                new Master.Application.Tenants.Queries.CheckSubdomainAvailability.SubdomainAvailabilityResponse(
                    "vanguarda", true)));

        Services.AddSingleton(_onboardingClientService);
        Services.AddSingleton(_authClientService);
        Services.AddSingleton<ITenantStateProvider>(_tenantStateProvider);
        Services.AddSingleton<ITenantSessionStateProvider>(_sessionProvider);
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
    /// Valida que ao avançar com documento fiscal inválido, uma mensagem de erro é exibida.
    /// </summary>
    [Fact]
    public void OnboardingPage_WhenAdvancingWithInvalidDocument_ShouldShowErrorMessage()
    {
        // Arrange
        var cut = Render<OnboardingPage>();

        cut.Find("#company-name").Change("Agência Beta");
        cut.Find("#company-cnpj").Change("12345678901"); // CPF inválido

        // Act
        var advanceButton = cut.Find(".wizard-actions .btn-wizard-primary");
        advanceButton.Click();

        // Assert
        var errorPill = cut.Find(".status-pill.error");
        errorPill.Should().NotBeNull();
        errorPill.TextContent.Should().Contain("inválido");
    }

    /// <summary>
    /// Valida que ao preencher Razão Social e CPF válido, o assistente avança para a Etapa 2 (Subdomínio).
    /// </summary>
    [Fact]
    public void OnboardingPage_WhenAdvancingWithValidCpf_ShouldProceedToStep2()
    {
        // Arrange
        var cut = Render<OnboardingPage>();

        cut.Find("#company-name").Change("Agência Beta");
        cut.Find("#company-cnpj").Change("52998224725"); // CPF válido

        // Act
        var advanceButton = cut.Find(".wizard-actions .btn-wizard-primary");
        advanceButton.Click();

        // Assert
        cut.Find("#subdomain-input").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que ao preencher Razão Social e CNPJ válido, o assistente avança para a Etapa 2 (Subdomínio).
    /// </summary>
    [Fact]
    public void OnboardingPage_WhenAdvancingWithValidCnpj_ShouldProceedToStep2()
    {
        // Arrange
        var cut = Render<OnboardingPage>();

        cut.Find("#company-name").Change("Vanguarda Digital Ltda");
        cut.Find("#company-cnpj").Change("12345678000195"); // CNPJ válido

        // Act
        var advanceButton = cut.Find(".wizard-actions .btn-wizard-primary");
        advanceButton.Click();

        // Assert
        cut.Find("#subdomain-input").Should().NotBeNull();
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
