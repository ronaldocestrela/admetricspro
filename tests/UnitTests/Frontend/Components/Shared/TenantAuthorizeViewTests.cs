using Bunit;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using UnitTests.Frontend.Common;
using WebApp.Components.Shared;
using Xunit;

namespace UnitTests.Frontend.Components.Shared;

/// <summary>
/// Testes unitários com bUnit para o componente de segurança visual <see cref="TenantAuthorizeView"/>.
/// </summary>
public sealed class TenantAuthorizeViewTests : BunitTestBase
{
    /// <summary>
    /// Valida que o conteúdo confidencial de margens financeiras é ocultado para usuários com papel Guest.
    /// </summary>
    [Fact]
    public void Render_QuandoUsuarioGuestEPermissaoMargens_DeveOcultarConteudo()
    {
        // Act
        var cut = Render<TenantAuthorizeView>(parameters =>
        {
            parameters.Add(p => p.Permission, TenantPermission.ViewFinancialMargins);
            parameters.Add(p => p.CurrentUserRole, TenantRole.Guest);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "secret-margins");
                builder.AddContent(2, "Markup da Agência: 30%");
                builder.CloseElement();
            }));
            parameters.Add(p => p.NotAuthorized, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "blocked-notice");
                builder.AddContent(2, "Acesso Restrito à Agência");
                builder.CloseElement();
            }));
        });

        // Assert
        cut.FindAll(".secret-margins").Should().BeEmpty();
        cut.Find(".blocked-notice").TextContent.Should().Be("Acesso Restrito à Agência");
    }

    /// <summary>
    /// Valida que o conteúdo confidencial de margens financeiras é exibido normalmente para usuários com papel Admin ou Owner.
    /// </summary>
    [Fact]
    public void Render_QuandoUsuarioAdminEPermissaoMargens_DeveExibirConteudo()
    {
        // Act
        var cut = Render<TenantAuthorizeView>(parameters =>
        {
            parameters.Add(p => p.Permission, TenantPermission.ViewFinancialMargins);
            parameters.Add(p => p.CurrentUserRole, TenantRole.Admin);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "secret-margins");
                builder.AddContent(2, "Markup da Agência: 30%");
                builder.CloseElement();
            }));
        });

        // Assert
        cut.Find(".secret-margins").TextContent.Should().Contain("Markup da Agência: 30%");
    }
}
