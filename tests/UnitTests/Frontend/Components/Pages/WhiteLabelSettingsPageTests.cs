using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages.Settings;
using WebApp.Models;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes de componente com bUnit para a página de White-Label (<see cref="WhiteLabelSettingsPage"/>).
/// </summary>
public sealed class WhiteLabelSettingsPageTests : BunitTestBase
{
    /// <summary>
    /// Valida que a página renderiza os títulos e campos de formulário para cores e logotipos.
    /// </summary>
    [Fact]
    public void WhiteLabelSettingsPage_ShouldRenderHeaderAndFormFields()
    {
        // Act
        var cut = Render<WhiteLabelSettingsPage>();

        // Assert
        cut.Find(".page-title").TextContent.Should().Contain("Personalização White-Label & Marca");
        cut.Find("#primaryColorPicker").Should().NotBeNull();
        cut.Find("#secondaryColorPicker").Should().NotBeNull();
        cut.Find("#lightLogoInput").Should().NotBeNull();
        cut.Find("#darkLogoInput").Should().NotBeNull();
        cut.Find("#faviconInput").Should().NotBeNull();
        cut.Find(".mockup-shell").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que a página carrega os dados iniciais do serviço de branding.
    /// </summary>
    [Fact]
    public void WhiteLabelSettingsPage_ShouldPopulateInitialDataFromClientService()
    {
        // Arrange
        TenantBrandingClientService.GetBrandingAsync(Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Tenants.Application.Branding.DTOs.TenantBrandingDetailsDto>.Success(
                new Tenants.Application.Branding.DTOs.TenantBrandingDetailsDto(
                    PrimaryColor: "#10B981",
                    SecondaryColor: "#1E293B",
                    LightLogoUrl: "https://cdn.example.com/brand-light.svg",
                    DarkLogoUrl: "https://cdn.example.com/brand-dark.svg",
                    FaviconUrl: "https://cdn.example.com/fav.png",
                    UpdatedAtUtc: DateTime.UtcNow)));

        // Act
        var cut = Render<WhiteLabelSettingsPage>();

        // Assert
        var primaryPicker = cut.Find("#primaryColorPicker");
        primaryPicker.GetAttribute("value").Should().Be("#10B981");

        var secondaryPicker = cut.Find("#secondaryColorPicker");
        secondaryPicker.GetAttribute("value").Should().Be("#1E293B");
    }

    /// <summary>
    /// Valida que o botão de salvar invoca o serviço de atualização e exibe mensagem de sucesso.
    /// </summary>
    [Fact]
    public async Task WhiteLabelSettingsPage_WhenSaving_ShouldCallUpdateBrandingAsyncAndShowSuccess()
    {
        // Arrange
        TenantBrandingClientService.UpdateBrandingAsync(Arg.Any<UpdateTenantBrandingModel>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Tenants.Application.Branding.DTOs.TenantBrandingDetailsDto>.Success(
                new Tenants.Application.Branding.DTOs.TenantBrandingDetailsDto(
                    PrimaryColor: "#10B981",
                    SecondaryColor: "#1E293B",
                    LightLogoUrl: null,
                    DarkLogoUrl: null,
                    FaviconUrl: null,
                    UpdatedAtUtc: DateTime.UtcNow)));

        var cut = Render<WhiteLabelSettingsPage>();

        // Act
        var saveButton = cut.Find("button.btn-primary");
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        await TenantBrandingClientService.Received(1).UpdateBrandingAsync(Arg.Any<UpdateTenantBrandingModel>(), Arg.Any<CancellationToken>());
        cut.Find(".alert-success").TextContent.Should().Contain("Identidade visual atualizada");
    }

    /// <summary>
    /// Valida que ao clicar em Restaurar Padrões os campos retornam aos valores padrão institucionais.
    /// </summary>
    [Fact]
    public async Task WhiteLabelSettingsPage_WhenResetClicked_ShouldResetToDefaultColors()
    {
        // Arrange
        var cut = Render<WhiteLabelSettingsPage>();

        // Act
        var resetButton = cut.Find("button.btn-secondary");
        await cut.InvokeAsync(() => resetButton.Click());

        // Assert
        var primaryPicker = cut.Find("#primaryColorPicker");
        primaryPicker.GetAttribute("value").Should().Be("#2563EB");
    }

    /// <summary>
    /// Valida que a página renderiza a seção de CNAME e o alvo DNS esperado.
    /// </summary>
    [Fact]
    public void WhiteLabelSettingsPage_ShouldRenderCnameSectionAndExpectedDnsTarget()
    {
        // Act
        var cut = Render<WhiteLabelSettingsPage>();

        // Assert
        cut.Find(".cname-card").Should().NotBeNull();
        cut.Find("#cnameExpectedTarget").TextContent.Should().Be("cname.admetricspro.com");
        cut.Find("#cnameStatusInactive").TextContent.Should().Be("Não Configurado");
    }

    /// <summary>
    /// Valida que quando o plano não suporta CNAME, o alerta de upgrade é exibido.
    /// </summary>
    [Fact]
    public void WhiteLabelSettingsPage_WhenPlanDoesNotSupportCname_ShouldShowUpgradeWarning()
    {
        // Arrange
        TenantCnameClientService.GetCnameDetailsAsync(Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Master.Application.Tenants.Queries.GetTenantCustomDomain.TenantCustomDomainDto>.Success(
                new Master.Application.Tenants.Queries.GetTenantCustomDomain.TenantCustomDomainDto(
                    TenantId: Guid.NewGuid(),
                    CustomDomain: null,
                    ExpectedCnameTarget: "cname.admetricspro.com",
                    IsConfigured: false,
                    HasPlanSupport: false)));

        // Act
        var cut = Render<WhiteLabelSettingsPage>();

        // Assert
        cut.Find("#cnamePlanWarning").TextContent.Should().Contain("upgrade para o plano Enterprise");
    }

    /// <summary>
    /// Valida que ao salvar o domínio customizado, o serviço de cliente CNAME é acionado.
    /// </summary>
    [Fact]
    public async Task WhiteLabelSettingsPage_WhenSavingCname_ShouldCallConfigureCnameAsync()
    {
        // Arrange
        TenantCnameClientService.ConfigureCnameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result.Success());

        var cut = Render<WhiteLabelSettingsPage>();

        var input = cut.Find("#cnameInput");
        input.Change("relatorios.agenciaalfa.com.br");

        // Act
        var saveButton = cut.Find("#btnSaveCname");
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        await TenantCnameClientService.Received(1).ConfigureCnameAsync("relatorios.agenciaalfa.com.br", Arg.Any<CancellationToken>());
        cut.Find(".alert-success").TextContent.Should().Contain("Domínio CNAME configurado com sucesso");
    }
}
