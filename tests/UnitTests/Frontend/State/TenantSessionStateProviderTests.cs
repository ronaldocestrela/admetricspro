using FluentAssertions;
using NSubstitute;
using Tenants.Application.Auth.DTOs;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.State;

/// <summary>
/// Testes unitários para o provedor de sessão do inquilino (<see cref="TenantSessionStateProvider"/>).
/// Valida inicialização, retenção de sessão autenticada, propagação de branding, persistência no storage e restauração pós-F5.
/// </summary>
public sealed class TenantSessionStateProviderTests
{
    private readonly TenantStateProvider _tenantStateProvider = new();
    private readonly IBrowserStorageService _storage = Substitute.For<IBrowserStorageService>();

    /// <summary>
    /// Valida que ao instanciar, a sessão inicia desautenticada.
    /// </summary>
    [Fact]
    public void InitialState_ShouldNotBeAuthenticated()
    {
        // Arrange
        var sut = new TenantSessionStateProvider(_tenantStateProvider, _storage);

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
        var sut = new TenantSessionStateProvider(_tenantStateProvider, _storage);
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
    /// Valida que ao definir a sessão assincronamente, ela é persistida no storage local.
    /// </summary>
    [Fact]
    public async Task SetSessionAsync_ShouldPersistSessionToStorage()
    {
        // Arrange
        var sut = new TenantSessionStateProvider(_tenantStateProvider, _storage);
        var dto = new AuthenticatedTenantUserDto(
            AccessToken: "jwt_token_456",
            TokenType: "Bearer",
            ExpiresIn: 3600,
            UserId: Guid.NewGuid(),
            Email: "gestor@agencia.com",
            FullName: "Marina Gestora",
            Role: "Admin",
            TenantId: Guid.NewGuid(),
            Subdomain: "agencia",
            Branding: null);

        // Act
        await sut.SetSessionAsync(dto);

        // Assert
        sut.IsAuthenticated.Should().BeTrue();
        await _storage.Received(1).SetItemAsync("admetricspro_tenant_session", dto, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida restauração de sessão prévia do storage do navegador (simulação pós-reload F5).
    /// </summary>
    [Fact]
    public async Task RestoreSessionAsync_WhenStorageHasData_ShouldRestoreSessionAndSyncTenantState()
    {
        // Arrange
        var dto = new AuthenticatedTenantUserDto(
            AccessToken: "jwt_token_restored",
            TokenType: "Bearer",
            ExpiresIn: 7200,
            UserId: Guid.NewGuid(),
            Email: "restaurado@agencia.com",
            FullName: "Ricardo Restaurado",
            Role: "Admin",
            TenantId: Guid.NewGuid(),
            Subdomain: "agencia-restaurada",
            Branding: new TenantBrandingDto("Agência Restaurada", "#10B981", "#059669", null, null, null));

        _storage.GetItemAsync<AuthenticatedTenantUserDto>("admetricspro_tenant_session", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AuthenticatedTenantUserDto?>(dto));

        var sut = new TenantSessionStateProvider(_tenantStateProvider, _storage);
        var sessionChangedFired = false;
        sut.OnSessionChanged += () => sessionChangedFired = true;

        // Act
        var restored = await sut.RestoreSessionAsync();

        // Assert
        restored.Should().BeTrue();
        sut.IsAuthenticated.Should().BeTrue();
        sut.CurrentSession.Should().Be(dto);
        sessionChangedFired.Should().BeTrue();
        _tenantStateProvider.CurrentTenant.TenantId.Should().Be(dto.TenantId);
        _tenantStateProvider.CurrentTenant.Name.Should().Be("Agência Restaurada");
    }

    /// <summary>
    /// Valida que RestoreSessionAsync retorna false caso o storage esteja vazio.
    /// </summary>
    [Fact]
    public async Task RestoreSessionAsync_WhenStorageEmpty_ShouldReturnFalse()
    {
        // Arrange
        _storage.GetItemAsync<AuthenticatedTenantUserDto>("admetricspro_tenant_session", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AuthenticatedTenantUserDto?>((AuthenticatedTenantUserDto?)null));

        var sut = new TenantSessionStateProvider(_tenantStateProvider, _storage);

        // Act
        var restored = await sut.RestoreSessionAsync();

        // Assert
        restored.Should().BeFalse();
        sut.IsAuthenticated.Should().BeFalse();
    }

    /// <summary>
    /// Valida que ao limpar a sessão, o estado volta para desautenticado e o storage é limpo.
    /// </summary>
    [Fact]
    public void ClearSession_ShouldResetSessionAndTenantStateAndClearStorage()
    {
        // Arrange
        var sut = new TenantSessionStateProvider(_tenantStateProvider, _storage);
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
        _storage.Received().RemoveItemAsync("admetricspro_tenant_session", Arg.Any<CancellationToken>());
    }
}
