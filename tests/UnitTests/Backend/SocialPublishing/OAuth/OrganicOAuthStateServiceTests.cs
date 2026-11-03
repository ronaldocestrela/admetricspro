using System.Text;
using System.Text.Json;
using FluentAssertions;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Infrastructure.OAuth;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.OAuth;

/// <summary>
/// Testes unitários do serviço de state OAuth orgânico anti-CSRF <see cref="OrganicOAuthStateService"/>.
/// </summary>
public sealed class OrganicOAuthStateServiceTests
{
    private const string TestSigningKey = "v3ryS3cur3AndLong3n0ughK3yForHmacSha256Signature!12345";
    private const string RedirectUri = "https://app.example.com/social/oauth/callback";
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly OrganicOAuthStateService _service = new(TestSigningKey);

    /// <summary>
    /// Emissão e validação recuperam tenant, workspace, plataforma, redirect e carimbo com TTL de 15 minutos.
    /// </summary>
    [Fact]
    public void Issue_AndValidate_DeveRecuperarPayloadComTtlDe15Minutos()
    {
        var issuedAt = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        var issue = _service.Issue(_tenantId, _workspaceId, OrganicPlatform.Instagram, RedirectUri, issuedAt);

        issue.State.Should().NotBeNullOrWhiteSpace();
        issue.ExpiresAtUtc.Should().Be(issuedAt.AddMinutes(15));

        var validate = _service.Validate(issue.State, _tenantId, _workspaceId, issuedAt.AddMinutes(1));
        validate.IsSuccess.Should().BeTrue();
        validate.Value.TenantId.Should().Be(_tenantId);
        validate.Value.WorkspaceId.Should().Be(_workspaceId);
        validate.Value.Platform.Should().Be(OrganicPlatform.Instagram);
        validate.Value.RedirectUri.Should().Be(RedirectUri);
        validate.Value.IssuedAtUtc.Should().Be(issuedAt);
    }

    /// <summary>
    /// Duas emissões no mesmo instante produzem states distintos por nonce independente.
    /// </summary>
    [Fact]
    public void Issue_ComMesmoContextoEInstante_ProduzStatesDistintos()
    {
        var issuedAt = DateTime.UtcNow;
        var first = _service.Issue(_tenantId, _workspaceId, OrganicPlatform.TikTok, RedirectUri, issuedAt);
        var second = _service.Issue(_tenantId, _workspaceId, OrganicPlatform.TikTok, RedirectUri, issuedAt);
        first.State.Should().NotBe(second.State);
    }

    /// <summary>
    /// O JSON do payload não contém tokens de acesso ou refresh.
    /// </summary>
    [Fact]
    public void Issue_PayloadSerializado_NaoContemTokensDeAcesso()
    {
        const string plantedSecret = "super-secret-access-token-plantado-no-teste";
        var issue = _service.Issue(_tenantId, _workspaceId, OrganicPlatform.FacebookPage, RedirectUri, DateTime.UtcNow);
        var payloadJson = DecodePayloadJson(issue.State);

        payloadJson.Should().NotContain("accessToken", because: "state não deve carregar access token");
        payloadJson.Should().NotContain("refreshToken", because: "state não deve carregar refresh token");
        payloadJson.Should().NotContain(plantedSecret);
    }

    /// <summary>
    /// State adulterado retorna Social.OAuthStateInvalid.
    /// </summary>
    [Fact]
    public void Validate_ComPayloadAdulterado_DeveRetornarOAuthStateInvalid()
    {
        var issue = _service.Issue(_tenantId, _workspaceId, OrganicPlatform.Instagram, RedirectUri, DateTime.UtcNow);
        var tampered = issue.State[..^4] + "XXXX";
        var result = _service.Validate(tampered, _tenantId, _workspaceId, DateTime.UtcNow);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OAuthStateInvalid.Code);
    }

    /// <summary>
    /// State com mais de 15 minutos retorna Social.OAuthStateInvalid.
    /// </summary>
    [Fact]
    public void Validate_ComEstadoExpirado_DeveRetornarOAuthStateInvalid()
    {
        var issuedAt = DateTime.UtcNow.AddMinutes(-16);
        var issue = _service.Issue(_tenantId, _workspaceId, OrganicPlatform.Instagram, RedirectUri, issuedAt);
        var result = _service.Validate(issue.State, _tenantId, _workspaceId, DateTime.UtcNow);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OAuthStateInvalid.Code);
    }

    /// <summary>
    /// State emitido no futuro além da folga retorna Social.OAuthStateInvalid.
    /// </summary>
    [Fact]
    public void Validate_ComTimestampFuturo_DeveRetornarOAuthStateInvalid()
    {
        var issuedAt = DateTime.UtcNow.AddMinutes(10);
        var issue = _service.Issue(_tenantId, _workspaceId, OrganicPlatform.Instagram, RedirectUri, issuedAt);
        var result = _service.Validate(issue.State, _tenantId, _workspaceId, DateTime.UtcNow);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OAuthStateInvalid.Code);
    }

    /// <summary>
    /// State de outro workspace retorna Social.OAuthStateInvalid.
    /// </summary>
    [Fact]
    public void Validate_ComOutroWorkspace_DeveRetornarOAuthStateInvalid()
    {
        var issue = _service.Issue(_tenantId, _workspaceId, OrganicPlatform.Instagram, RedirectUri, DateTime.UtcNow);
        var result = _service.Validate(issue.State, _tenantId, Guid.NewGuid(), DateTime.UtcNow);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OAuthStateInvalid.Code);
    }

    /// <summary>
    /// State de outro tenant retorna Social.OAuthStateInvalid.
    /// </summary>
    [Fact]
    public void Validate_ComOutroTenant_DeveRetornarOAuthStateInvalid()
    {
        var issue = _service.Issue(_tenantId, _workspaceId, OrganicPlatform.Instagram, RedirectUri, DateTime.UtcNow);
        var result = _service.Validate(issue.State, Guid.NewGuid(), _workspaceId, DateTime.UtcNow);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OAuthStateInvalid.Code);
    }

    /// <summary>
    /// Strings de state inválidas retornam Social.OAuthStateInvalid.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invalid.token.format.extra")]
    public void Validate_ComStringInvalida_DeveRetornarOAuthStateInvalid(string? invalidState)
    {
        var result = _service.Validate(invalidState!, _tenantId, _workspaceId, DateTime.UtcNow);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.OAuthStateInvalid.Code);
    }

    private static string DecodePayloadJson(string state)
    {
        var parts = state.Split('.');
        var payloadBase64Url = parts[0];
        var payloadBytes = Base64UrlDecode(payloadBase64Url);
        return Encoding.UTF8.GetString(payloadBytes);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
