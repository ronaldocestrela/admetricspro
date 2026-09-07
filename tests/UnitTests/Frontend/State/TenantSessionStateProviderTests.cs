using FluentAssertions;
using Tenants.Application.Auth.DTOs;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.State;

/// <summary>
/// Testes unitários para o provedor de sessão do inquilino (<see cref="TenantSessionStateProvider"/>).
/// Valida inicialização, retenção de sessão autenticada, propagação de branding e encerramento de sessão.
/// </summary>
public sealed class TenantSessionStateProviderTests
{
    private readonly TenantStateProvider _tenantStateProvider = new();

    /// <summary>
    /// Valida que ao instanciar, a sessão inicia desautenticada.
    /// </summary>
    [Fact]
    public void InitialState_ShouldNotBeAuthenticated()
    {
        // Arrange
        var sut = new TenantSessionStateProvider(_tenantStateProvider);

        // Assert
        sut.IsAuthenticated.Should().BeFalse();
        sut.CurrentSession.Should().BeNull();
    }

    /// <summary>
    /// Valida que ao definir a sessão, IsAuthenticated torna-se true e o TenantStateProvider é sincronizado com as cores da agência.
    /// </summary>
    [Fact]
    public void SetSession_ShouldUpdateSessionAndSyncTenantState()
    {
        // Arrange
        var sut = new TenantSessionStateProvider(_tenantStateProvider);
        var sessionChangedFired = false;
        sut.OnSessionChanged += () => sessionChangedFired = true;

        var dto = new AuthenticatedTenantUserDto(
            AccessToken: "jwt_token_123",
            TokenType: "Bearer",
            ExpiresIn: 28800,
            UserId: Guid.NewGuid(),
            Email: "gestor@vanguarda.com.br",
            FullName: "Carlos Gestor",
            Role: "Owner",
            TenantId: Guid.NewGuid(),
            Subdomain: "vanguarda",
            Branding: new TenantBrandingDto("Agência Vanguarda", "#1E40AF", "#F59E0B", null, null, null));

        // Act
        sut.SetSession(dto);

        // Assert
        sut.IsAuthenticated.Should().BeTrue();
        sut.CurrentSession.Should().Be(dto);
        sessionChangedFired.Should().BeTrue();

        _tenantStateProvider.CurrentTenant.TenantId.Should().Be(dto.TenantId);
        _tenantStateProvider.CurrentTenant.Name.Should().Be("Agência Vanguarda");
        _tenantStateProvider.CurrentTenant.Branding.PrimaryColor.Should().Be("#1E40AF");
    }

    /// <summary>
    /// Valida que ao limpar a sessão, o estado volta para desautenticado e o TenantState volta ao padrão.
    /// </summary>
    [Fact]
    public void ClearSession_ShouldResetSessionAndTenantState()
    {
        // Arrange
        var sut = new TenantSessionStateProvider(_tenantStateProvider);
        var dto = new AuthenticatedTenantUserDto(
            AccessToken: "jwt_token_123",
            TokenType: "Bearer",
            ExpiresIn: 28800,
            UserId: Guid.NewGuid(),
            Email: "gestor@vanguarda.com.br",
            FullName: "Carlos Gestor",
            Role: "Owner",
            TenantId: Guid.NewGuid(),
            Subdomain: "vanguarda",
            Branding: null);

        sut.SetSession(dto);

        // Act
        sut.ClearSession();

        // Assert
        sut.IsAuthenticated.Should().BeFalse();
        sut.CurrentSession.Should().BeNull();
        _tenantStateProvider.CurrentTenant.TenantId.Should().Be(Guid.Empty);
    }
}
