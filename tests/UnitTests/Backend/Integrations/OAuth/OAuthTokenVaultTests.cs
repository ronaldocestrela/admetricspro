using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Domain.OAuth;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>
/// Testes unitários para a entidade de domínio <see cref="OAuthTokenVault"/>.
/// </summary>
public sealed class OAuthTokenVaultTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private const string Platform = "MetaAds";
    private const string ExternalAccountId = "act_1020304050";
    private const string ExternalAccountName = "Conta Principal Meta";
    private const string EncryptedAccessToken = "EncryptedSecretTokenBase64==";
    private const string EncryptedRefreshToken = "EncryptedRefreshTokenBase64==";
    private const string Scopes = "ads_read,ads_management";

    /// <summary>
    /// Valida criação bem-sucedida de um registro no cofre com status Active.
    /// </summary>
    [Fact]
    public void Create_ComParametrosValidos_DeveRetornarSucessoComStatusAtivo()
    {
        // Act
        var result = OAuthTokenVault.Create(
            Guid.NewGuid(),
            _workspaceId,
            Platform,
            ExternalAccountId,
            ExternalAccountName,
            EncryptedAccessToken,
            EncryptedRefreshToken,
            DateTime.UtcNow.AddDays(60),
            DateTime.UtcNow.AddYears(1),
            Scopes);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.WorkspaceId.Should().Be(_workspaceId);
        result.Value.Platform.Should().Be(Platform);
        result.Value.ExternalAccountId.Should().Be(ExternalAccountId);
        result.Value.ExternalAccountName.Should().Be(ExternalAccountName);
        result.Value.EncryptedAccessToken.Should().Be(EncryptedAccessToken);
        result.Value.EncryptedRefreshToken.Should().Be(EncryptedRefreshToken);
        result.Value.Status.Should().Be(OAuthConnectionStatus.Active);
        result.Value.Scopes.Should().Be(Scopes);
    }

    /// <summary>
    /// Valida que valores nulos ou em branco para plataforma retornam erro de validação.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_ComPlatformInvalida_DeveRetornarFalhaValidacao(string? invalidPlatform)
    {
        // Act
        var result = OAuthTokenVault.Create(
            Guid.NewGuid(),
            _workspaceId,
            invalidPlatform!,
            ExternalAccountId,
            ExternalAccountName,
            EncryptedAccessToken,
            EncryptedRefreshToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OAuthTokenVault.InvalidPlatform");
    }

    /// <summary>
    /// Valida que plataformas desconhecidas retornam falha de plataforma não suportada.
    /// </summary>
    [Theory]
    [InlineData("InvalidPlatformX")]
    [InlineData("TwitterAds")]
    public void Create_ComPlataformaNaoSuportada_DeveRetornarFalha(string unsupportedPlatform)
    {
        // Act
        var result = OAuthTokenVault.Create(
            Guid.NewGuid(),
            _workspaceId,
            unsupportedPlatform,
            ExternalAccountId,
            ExternalAccountName,
            EncryptedAccessToken,
            EncryptedRefreshToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OAuthTokenVault.UnsupportedPlatform");
    }

    /// <summary>
    /// Valida que workspace vazio retorna erro.
    /// </summary>
    [Fact]
    public void Create_ComWorkspaceIdVazio_DeveRetornarFalha()
    {
        // Act
        var result = OAuthTokenVault.Create(
            Guid.NewGuid(),
            Guid.Empty,
            Platform,
            ExternalAccountId,
            ExternalAccountName,
            EncryptedAccessToken,
            EncryptedRefreshToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OAuthTokenVault.EmptyWorkspaceId");
    }

    /// <summary>
    /// Valida que token vazio retorna erro de validação.
    /// </summary>
    [Fact]
    public void Create_ComEncryptedAccessTokenVazio_DeveRetornarFalha()
    {
        // Act
        var result = OAuthTokenVault.Create(
            Guid.NewGuid(),
            _workspaceId,
            Platform,
            ExternalAccountId,
            ExternalAccountName,
            string.Empty,
            EncryptedRefreshToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OAuthTokenVault.EmptyAccessToken");
    }

    /// <summary>
    /// Valida atualização de tokens e reativação do status.
    /// </summary>
    [Fact]
    public void UpdateTokens_ComNovosTokens_DeveAtualizarValoresEStatus()
    {
        // Arrange
        var vault = OAuthTokenVault.Create(
            Guid.NewGuid(),
            _workspaceId,
            Platform,
            ExternalAccountId,
            ExternalAccountName,
            EncryptedAccessToken,
            EncryptedRefreshToken).Value;

        var newAccessToken = "NewEncryptedAccessTokenBase64==";
        var newRefreshToken = "NewEncryptedRefreshTokenBase64==";
        var newExpiration = DateTime.UtcNow.AddDays(90);

        // Act
        var updateResult = vault.UpdateTokens(newAccessToken, newRefreshToken, newExpiration, null);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        vault.EncryptedAccessToken.Should().Be(newAccessToken);
        vault.EncryptedRefreshToken.Should().Be(newRefreshToken);
        vault.AccessTokenExpiresAtUtc.Should().Be(newExpiration);
        vault.Status.Should().Be(OAuthConnectionStatus.Active);
        vault.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Valida a detecção preventiva de expiração iminente de token.
    /// </summary>
    [Fact]
    public void IsExpiringSoon_QuandoExpiraDentroDoLimite_DeveRetornarTrue()
    {
        // Arrange
        var vault = OAuthTokenVault.Create(
            Guid.NewGuid(),
            _workspaceId,
            Platform,
            ExternalAccountId,
            ExternalAccountName,
            EncryptedAccessToken,
            EncryptedRefreshToken,
            accessTokenExpiresAtUtc: DateTime.UtcNow.AddHours(24)).Value;

        // Act & Assert
        vault.IsExpiringSoon(TimeSpan.FromHours(48)).Should().BeTrue();
        vault.IsExpiringSoon(TimeSpan.FromHours(12)).Should().BeFalse();
    }

    /// <summary>
    /// Valida revogação da conexão e alteração de status.
    /// </summary>
    [Fact]
    public void Revoke_DeveAlterarStatusParaRevoked()
    {
        // Arrange
        var vault = OAuthTokenVault.Create(
            Guid.NewGuid(),
            _workspaceId,
            Platform,
            ExternalAccountId,
            ExternalAccountName,
            EncryptedAccessToken,
            EncryptedRefreshToken).Value;

        // Act
        vault.Revoke();

        // Assert
        vault.Status.Should().Be(OAuthConnectionStatus.Revoked);
        vault.UpdatedAtUtc.Should().NotBeNull();
    }
}
