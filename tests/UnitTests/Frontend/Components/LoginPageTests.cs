using BackofficeApp.Components.Pages;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using UnitTests.Frontend.Common;
using Xunit;

namespace UnitTests.Frontend.Components;

/// <summary>
/// Testes de componente bUnit para a tela de autenticação do Backoffice (LoginPage).
/// </summary>
public sealed class LoginPageTests : BunitTestBase
{
    /// <summary>
    /// Valida a renderização básica do formulário de login com campos e botão de envio.
    /// </summary>
    [Fact]
    public void LoginPage_ShouldRenderFormInputsAndSubmitButton()
    {
        // Act
        var cut = Render<LoginPage>();

        // Assert
        cut.Find("input[type='email']").Should().NotBeNull();
        cut.Find("input[type='password']").Should().NotBeNull();
        cut.Find("button[type='submit']").TextContent.Should().Contain("Entrar no Backoffice");
    }

    /// <summary>
    /// Valida que a mensagem de erro é exibida com destaque quando passada como parâmetro na URL.
    /// </summary>
    [Fact]
    public void LoginPage_WhenErrorMessageProvided_ShouldRenderAlertError()
    {
        // Arrange
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        nav.NavigateTo("/login?error=Credenciais%20inv%C3%A1lidas");

        // Act
        var cut = Render<LoginPage>();

        // Assert
        var alert = cut.Find(".alert-error");
        alert.TextContent.Should().Contain("Credenciais inválidas");
    }

    /// <summary>
    /// Valida que o campo oculto returnUrl reflete o parâmetro fornecido na query da URL.
    /// </summary>
    [Fact]
    public void LoginPage_WhenReturnUrlProvided_ShouldPopulateHiddenField()
    {
        // Arrange
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        nav.NavigateTo("/login?returnUrl=%2Ftenants");

        // Act
        var cut = Render<LoginPage>();

        // Assert
        var returnInput = cut.Find("input[name='returnUrl']");
        returnInput.GetAttribute("value").Should().Be("/tenants");
    }
}
