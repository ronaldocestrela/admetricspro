using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Tenants.Infrastructure.Auth;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o serviço de emissão de tokens JWT de inquilinos (TenantJwtTokenService).
/// </summary>
public sealed class TenantJwtTokenServiceTests
{
    private readonly TenantJwtOptions _validOptions = new()
    {
        SecretKey = "super_secret_signing_key_at_least_32_characters_long_2026!",
        Issuer = "AdMetricsPro.Tenants.Test",
        Audience = "AdMetricsPro.ClientApp.Test",
        ExpirationMinutes = 120
    };

    /// <summary>
    /// Valida que a inicialização falha se a chave secreta for nula, vazia ou menor que 32 caracteres.
    /// </summary>
    /// <param name="invalidSecret">Chave secreta inválida.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("curta_demais")]
    public void Constructor_WithInvalidSecretKey_ShouldThrowInvalidOperationException(string? invalidSecret)
    {
        // Arrange
        var options = new TenantJwtOptions { SecretKey = invalidSecret! };
        var optionsWrapper = Options.Create(options);

        // Act
        var act = () => new TenantJwtTokenService(optionsWrapper);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*32 characters*");
    }

    /// <summary>
    /// Valida que um token válido é gerado com todas as claims necessárias de identidade e contexto de tenant.
    /// </summary>
    [Fact]
    public void GenerateToken_WithValidUserAndTenant_ShouldReturnSignedJwtWithExpectedClaims()
    {
        // Arrange
        var sut = new TenantJwtTokenService(Options.Create(_validOptions));
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var subdomain = "vanguarda";

        var user = TenantUser.Create(
            userId,
            "Carlos Gestor",
            "carlos@vanguarda.com.br",
            null,
            "dummy_hash",
            TenantRole.Owner).Value;

        // Act
        var result = sut.GenerateToken(user, tenantId, subdomain);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Value);

        token.Issuer.Should().Be(_validOptions.Issuer);
        token.Audiences.Should().Contain(_validOptions.Audience);

        // Claims essenciais
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "carlos@vanguarda.com.br");
        token.Claims.Should().Contain(c => c.Type == "name" && c.Value == "Carlos Gestor");
        token.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == tenantId.ToString());
        token.Claims.Should().Contain(c => c.Type == "tenant_subdomain" && c.Value == subdomain);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Owner");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }
}
