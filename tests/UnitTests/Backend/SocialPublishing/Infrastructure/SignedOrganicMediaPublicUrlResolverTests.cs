using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Infrastructure.Media;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Testes do resolvedor de URL pública assinada.
/// </summary>
public sealed class SignedOrganicMediaPublicUrlResolverTests
{
    private readonly Guid _tenantId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    /// <summary>
    /// Sem PublicBaseUrl retorna falha de configuração.
    /// </summary>
    [Fact]
    public void ResolvePublicUrl_SemBaseUrl_DeveFalharConfiguracao()
    {
        var resolver = CreateResolver(publicBaseUrl: string.Empty);

        var result = resolver.ResolvePublicUrl(
            $"organic-media/durable/{_tenantId:N}/{Guid.NewGuid():N}/{Guid.NewGuid():N}",
            "image/png");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.MediaPublicBaseUrlMissing.Code);
    }

    /// <summary>
    /// Com base URL válida, retorna link da API e não a chave crua.
    /// </summary>
    [Fact]
    public void ResolvePublicUrl_ComBaseUrl_DeveRetornarLinkDaApi()
    {
        var objectKey = $"organic-media/durable/{_tenantId:N}/{Guid.NewGuid():N}/{Guid.NewGuid():N}";
        var resolver = CreateResolver("https://api.test");

        var result = resolver.ResolvePublicUrl(objectKey, "image/png");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("https://api.test/api/v1/public/media/");
        result.Value.Should().NotContain(objectKey);
    }

    private SignedOrganicMediaPublicUrlResolver CreateResolver(string publicBaseUrl)
    {
        ITenantContextAccessor accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(_tenantId, "slug", TenantResolutionSource.Header),
        };

        return new SignedOrganicMediaPublicUrlResolver(
            Options.Create(new ObjectStorageOptions
            {
                PublicBaseUrl = publicBaseUrl,
                PublishUrlSigningKey = "organic-media-publish-signing-key-32-chars!",
                PublishUrlTtlMinutes = 120,
            }),
            Options.Create(new OrganicMediaOptions()),
            accessor,
            new OrganicMediaPublishTokenService(
                Options.Create(new ObjectStorageOptions
                {
                    PublishUrlSigningKey = "organic-media-publish-signing-key-32-chars!",
                    PublishUrlTtlMinutes = 120,
                }),
                Options.Create(new OrganicMediaOptions())),
            TimeProvider.System);
    }
}