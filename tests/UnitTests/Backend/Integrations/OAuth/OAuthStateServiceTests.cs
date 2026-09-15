using FluentAssertions;
using Integrations.Domain.OAuth;
using Integrations.Infrastructure.OAuth;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>
/// Testes unitários para o gerador e validador de estado OAuth anti-CSRF <see cref="OAuthStateService"/>.
/// </summary>
public sealed class OAuthStateServiceTests
{
    private const string TestSigningKey = "v3ryS3cur3AndLong3n0ughK3yForHmacSha256Signature!12345";
    private readonly OAuthStateService _service = new(TestSigningKey);
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private const string Platform = OAuthPlatform.MetaAds;
    private const string RedirectUri = "https://app.admetricspro.com/integrations/oauth/callback";

    /// <summary>
    /// Valida que um estado gerado com parâmetros válidos é desembalado com integridade total.
    /// </summary>
    [Fact]
    public void GenerateState_E_ValidateAndUnpackState_DeveRecuperarDadosOriginais()
    {
        // Act
        var state = _service.GenerateState(_tenantId, _workspaceId, Platform, RedirectUri);
        var unpackResult = _service.ValidateAndUnpackState(state);

        // Assert
        state.Should().NotBeNullOrWhiteSpace();
        unpackResult.IsSuccess.Should().BeTrue();
        unpackResult.Value.TenantId.Should().Be(_tenantId);
        unpackResult.Value.WorkspaceId.Should().Be(_workspaceId);
        unpackResult.Value.Platform.Should().Be(Platform);
        unpackResult.Value.RedirectUri.Should().Be(RedirectUri);
        unpackResult.Value.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Valida que alteração no payload do estado invalida a assinatura HMAC e resulta em falha de segurança.
    /// </summary>
    [Fact]
    public void ValidateAndUnpackState_ComPayloadAdulterado_DeveRetornarFalhaAssinatura()
    {
        // Arrange
        var state = _service.GenerateState(_tenantId, _workspaceId, Platform, RedirectUri);
        var tamperedState = state.Substring(0, state.Length - 4) + "XXXX";

        // Act
        var result = _service.ValidateAndUnpackState(tamperedState);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OAuthState.InvalidSignature");
    }

    /// <summary>
    /// Valida que estados com mais de 15 minutos são rejeitados por expiração temporal.
    /// </summary>
    [Fact]
    public void ValidateAndUnpackState_ComEstadoExpirado_DeveRetornarFalhaExpiracao()
    {
        // Arrange
        var expiredCreatedAt = DateTime.UtcNow.AddMinutes(-20);
        var state = _service.GenerateStateWithTimestamp(_tenantId, _workspaceId, Platform, RedirectUri, expiredCreatedAt);

        // Act
        var result = _service.ValidateAndUnpackState(state);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OAuthState.Expired");
    }

    /// <summary>
    /// Valida que estados malformados retornam erro de formato inválido.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invalid.token.format")]
    public void ValidateAndUnpackState_ComStringInvalida_DeveRetornarFalhaFormato(string? invalidState)
    {
        // Act
        var result = _service.ValidateAndUnpackState(invalidState!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OAuthState.InvalidFormat");
    }
}
