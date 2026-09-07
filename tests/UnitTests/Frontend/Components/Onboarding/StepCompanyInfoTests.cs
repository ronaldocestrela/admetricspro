using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Onboarding;
using WebApp.Models;
using WebApp.Services;
using Xunit;

namespace UnitTests.Frontend.Components.Onboarding;

/// <summary>
/// Testes unitários com bUnit para o componente <see cref="StepCompanyInfo"/>
/// validando formatação de máscara e diagnóstico em tempo real de CPF ou CNPJ.
/// </summary>
public sealed class StepCompanyInfoTests : BunitTestBase
{
    private readonly ITenantOnboardingClientService _onboardingClientService = Substitute.For<ITenantOnboardingClientService>();

    public StepCompanyInfoTests()
    {
        _onboardingClientService.CheckTaxDocumentAvailabilityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Master.Application.Tenants.Queries.CheckTaxDocumentAvailability.TaxDocumentAvailabilityResponse>.Success(
                new Master.Application.Tenants.Queries.CheckTaxDocumentAvailability.TaxDocumentAvailabilityResponse(
                    "52998224725", "529.982.247-25", true, true, "CPF")));

        Services.AddSingleton(_onboardingClientService);
    }

    /// <summary>
    /// Valida que o componente renderiza o input de documento com o placeholder de CPF ou CNPJ.
    /// </summary>
    [Fact]
    public void StepCompanyInfo_ShouldRenderDocumentInputWithCorrectPlaceholder()
    {
        // Arrange
        var model = new TenantOnboardingFormModel();

        // Act
        var cut = Render<StepCompanyInfo>(parameters => parameters
            .Add(p => p.Model, model));

        // Assert
        var input = cut.Find("#company-cnpj");
        input.Should().NotBeNull();
        input.GetAttribute("placeholder").Should().Contain("000.000.000-00 ou 00.000.000/0000-00");
    }

    /// <summary>
    /// Valida que ao digitar um CPF inválido, um status pill com erro é exibido imediatamente.
    /// </summary>
    [Fact]
    public void StepCompanyInfo_WhenTypingInvalidCpf_ShouldShowErrorStatusPill()
    {
        // Arrange
        var model = new TenantOnboardingFormModel();
        var cut = Render<StepCompanyInfo>(parameters => parameters
            .Add(p => p.Model, model));

        // Act - digita CPF inválido
        var input = cut.Find("#company-cnpj");
        input.Change("12345678901");

        // Assert
        var errorPill = cut.Find(".status-pill.error");
        errorPill.Should().NotBeNull();
        errorPill.TextContent.Should().Contain("inválido");
    }

    /// <summary>
    /// Valida que ao digitar um CPF válido e disponível, a máscara é aplicada e o pill de sucesso é exibido.
    /// </summary>
    [Fact]
    public void StepCompanyInfo_WhenTypingValidCpf_ShouldFormatAndShowSuccessStatusPill()
    {
        // Arrange
        var model = new TenantOnboardingFormModel();
        var cut = Render<StepCompanyInfo>(parameters => parameters
            .Add(p => p.Model, model));

        // Act - digita CPF válido
        var input = cut.Find("#company-cnpj");
        input.Change("52998224725");

        // Assert
        model.Cnpj.Should().Be("529.982.247-25");
        var successPill = cut.Find(".status-pill.success");
        successPill.Should().NotBeNull();
        successPill.TextContent.Should().Contain("disponível");
    }

    /// <summary>
    /// Valida que quando a API retorna que o documento já está em uso, um status pill de erro é exibido.
    /// </summary>
    [Fact]
    public void StepCompanyInfo_WhenDocumentAlreadyInUse_ShouldShowErrorStatusPill()
    {
        // Arrange
        _onboardingClientService.CheckTaxDocumentAvailabilityAsync("12345678000195", Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Master.Application.Tenants.Queries.CheckTaxDocumentAvailability.TaxDocumentAvailabilityResponse>.Success(
                new Master.Application.Tenants.Queries.CheckTaxDocumentAvailability.TaxDocumentAvailabilityResponse(
                    "12345678000195", "12.345.678/0001-95", true, false, "CNPJ", "O CNPJ informado já está em uso por outro inquilino cadastrado.")));

        var model = new TenantOnboardingFormModel();
        var cut = Render<StepCompanyInfo>(parameters => parameters
            .Add(p => p.Model, model));

        // Act - digita CNPJ em uso
        var input = cut.Find("#company-cnpj");
        input.Change("12345678000195");

        // Assert
        var errorPill = cut.Find(".status-pill.error");
        errorPill.Should().NotBeNull();
        errorPill.TextContent.Should().Contain("já está em uso");
    }
}
