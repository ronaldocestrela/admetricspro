using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Infrastructure.Media;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Testes de emissão e validação de token HMAC de mídia pública.
/// </summary>
public sealed class OrganicMediaPublishTokenServiceTests
{
    private const string SigningKey = "organic-media-publish-signing-key-32-chars!";
    private readonly Guid _tenantId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    /// <summary>
    /// URL resolver deve incluir segmento da API e não a chave crua.
    /// </summary>
    [Fact]
    public void ResolvePublicUrl_DeveEmitirUrlComToken()
    {
        var key = $"organic-media/durable/{_tenantId:N}/{Guid.NewGuid():N}/{Guid.NewGuid():N}";
        var service = CreateTokenService(120);
        var token = service.IssueToken(_tenantId, key, "image/png", DateTime.UtcNow);
        token.IsSuccess.Should().BeTrue();
        token.Value.Should().NotContain("organic-media/");
        token.Value.Should().Contain(".");
    }

    /// <summary>
    /// Token expirado falha na validação.
    /// </summary>
    [Fact]
    public void ValidateToken_Expirado_DeveFalhar()
    {
        var key = $"organic-media/durable/{_tenantId:N}/{Guid.NewGuid():N}/{Guid.NewGuid():N}";
        var issuedAt = DateTime.UtcNow;
        var service = CreateTokenService(ttlMinutes: 1);
        var token = service.IssueToken(_tenantId, key, "image/jpeg", issuedAt).Value;

        var result = service.ValidateToken(token, issuedAt.AddMinutes(10));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PublicMediaNotFound.Code);
    }

    /// <summary>
    /// TTL acima do teto é limitado a 1440 minutos na emissão.
    /// </summary>
    [Fact]
    public void IssueToken_TtlAcimaDoTeto_Usa1440Minutos()
    {
        var key = $"organic-media/durable/{_tenantId:N}/{Guid.NewGuid():N}/{Guid.NewGuid():N}";
        var issuedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var service = CreateTokenService(ttlMinutes: 5000);
        var token = service.IssueToken(_tenantId, key, "image/png", issuedAt).Value;

        var validated = service.ValidateToken(token, issuedAt.AddMinutes(1439));
        validated.IsSuccess.Should().BeTrue();

        var expired = service.ValidateToken(token, issuedAt.AddMinutes(1441));
        expired.IsFailure.Should().BeTrue();
    }

    private OrganicMediaPublishTokenService CreateTokenService(int ttlMinutes) =>
        new(
            Options.Create(new ObjectStorageOptions
            {
                PublishUrlSigningKey = SigningKey,
                PublishUrlTtlMinutes = ttlMinutes,
            }),
            Options.Create(new OrganicMediaOptions()));
}