using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tenants.Application.Auth.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;
using WebApp.Models;
using WebApp.Services;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Components;

/// <summary>
/// Testes unitários com bUnit para o componente de autenticação de inquilinos (<see cref="TenantLoginPage"/>).
/// Valida renderização, resolução automática de subdomínio, validação reativa e fluxo de login com o padrão Result.
/// </summary>
public sealed class TenantLoginPageTests : BunitTestBase
{
    private readonly ITenantAuthClientService _authClientService = Substitute.For<ITenantAuthClientService>();

    public TenantLoginPageTests()
    {
        Services.AddSingleton<ITenantAuthClientService>(_authClientService);
    }

    /// <summary>
    /// Valida que a tela de login renderiza campos básicos de e-mail, senha e botão de submissão.
    /// </summary>
    [Fact]
    public void TenantLoginPage_ShouldRenderInputsAndSubmitButton()
    {
        // Act
        var cut = Render<TenantLoginPage>();

        // Assert
        cut.Find("input[type='email']").Should().NotBeNull();
        cut.Find("input[type='password']").Should().NotBeNull();
        cut.Find("button[type='submit']").Should().NotBeNull();
        cut.Find("button[type='submit']").TextContent.Should().Contain("Entrar");
    }

    /// <summary>
    /// Valida que quando o subdomínio é passado como query parameter, os metadados de branding são carregados dinamicamente.
    /// </summary>
    [Fact]
    public void TenantLoginPage_WhenSubdomainQueryProvided_ShouldFetchBrandingAndApplyTheme()
    {
        // Arrange
        var brandingViewModel = new TenantPublicBrandingViewModel(
            TenantId: Guid.NewGuid(),
            CompanyName: "Vanguarda Performance",
            Subdomain: "vanguarda",
            CustomDomain: null,
            PrimaryColor: "#1E40AF",
            SecondaryColor: "#F59E0B",
            LogoUrl: null,
            IsActive: true);

        _authClientService.GetPublicBrandingAsync("vanguarda", Arg.Any<CancellationToken>())
            .Returns(Result<TenantPublicBrandingViewModel>.Success(brandingViewModel));

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost:5000/login?subdomain=vanguarda");

        // Act
        var cut = Render<TenantLoginPage>();

        // Assert
        cut.Markup.Should().Contain("Vanguarda Performance");
        cut.Find(".login-card").GetAttribute("style").Should().Contain("--tenant-primary: #1E40AF");
    }

    /// <summary>
    /// Valida que ao submeter o formulário sem preencher os campos, exibe alerta de erro de validação reativa.
    /// </summary>
    [Fact]
    public void TenantLoginPage_WhenSubmittingEmptyForm_ShouldDisplayValidationAlert()
    {
        // Arrange
        var cut = Render<TenantLoginPage>();

        // Act
        var submitButton = cut.Find("button[type='submit']");
        submitButton.Click();

        // Assert
        var errorAlert = cut.Find(".alert-error");
        errorAlert.Should().NotBeNull();
        errorAlert.TextContent.Should().Contain("e-mail");
    }

    /// <summary>
    /// Valida que ao fornecer credenciais válidas, executa o login, define a sessão e redireciona para o dashboard.
    /// </summary>
    [Fact]
    public void TenantLoginPage_WhenValidCredentialsSubmitted_ShouldCallLoginAndNavigate()
    {
        // Arrange
        var userDto = new AuthenticatedTenantUserDto(
            AccessToken: "jwt_token_sucesso",
            TokenType: "Bearer",
            ExpiresIn: 28800,
            UserId: Guid.NewGuid(),
            Email: "gestor@vanguarda.com.br",
            FullName: "Carlos Gestor",
            Role: "Owner",
            TenantId: Guid.NewGuid(),
            Subdomain: "vanguarda",
            Branding: new TenantBrandingDto("Vanguarda Performance", "#1E40AF", "#F59E0B", null, null, null));

        _authClientService.LoginAsync(Arg.Any<TenantLoginModel>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedTenantUserDto>.Success(userDto));

        var cut = Render<TenantLoginPage>();

        cut.Find("input[type='email']").Change("gestor@vanguarda.com.br");
        cut.Find("input[type='password']").Change("SenhaValida123!");

        // Act
        var submitButton = cut.Find("button[type='submit']");
        submitButton.Click();

        // Assert
        TenantSessionStateProvider.IsAuthenticated.Should().BeTrue();
        TenantSessionStateProvider.CurrentSession.Should().Be(userDto);

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.Uri.Should().Contain("/dashboard");
    }

    /// <summary>
    /// Valida que quando o login falha na API (ex: credenciais inválidas), o erro retornado no Result é exibido ao usuário.
    /// </summary>
    [Fact]
    public void TenantLoginPage_WhenLoginFails_ShouldDisplayErrorMessage()
    {
        // Arrange
        _authClientService.LoginAsync(Arg.Any<TenantLoginModel>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedTenantUserDto>.Failure(
                BuildingBlocks.Domain.Primitives.Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha incorretos.")));

        var cut = Render<TenantLoginPage>();

        cut.Find("input[type='email']").Change("gestor@vanguarda.com.br");
        cut.Find("input[type='password']").Change("SenhaIncorreta");

        // Act
        var submitButton = cut.Find("button[type='submit']");
        submitButton.Click();

        // Assert
        var errorAlert = cut.Find(".alert-error");
        errorAlert.Should().NotBeNull();
        errorAlert.TextContent.Should().Contain("E-mail ou senha incorretos.");
    }
}
