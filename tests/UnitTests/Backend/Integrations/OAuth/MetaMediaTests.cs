using System.Net;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Infrastructure.Meta;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Upload deve validar bytes e manter credenciais fora de URLs.</summary>
public sealed class MetaMediaTests
{
    /// <summary>Arquivos disfarçados não devem ser enviados para a Meta.</summary>
    [Fact]
    public async Task UploadAsync_InvalidPng_IsRejectedWithoutHttp()
    {
        var called = false;
        var api = new MetaMediaApi(new HttpClient(new TestHttpMessageHandler((_, _) =>
        { called = true; return new(HttpStatusCode.OK); })), Options.Create(new MetaAdsOAuthOptions()));
        var result = await api.UploadAsync("act_11", "token", "image/png", "file.png", new byte[] { 1, 2, 3 }, default);
        result.IsFailure.Should().BeTrue();
        called.Should().BeFalse();
    }

    /// <summary>Um hash de imagem confirmado pode ser usado na criação de criativos.</summary>
    [Fact]
    public async Task UploadAsync_ValidImage_ReturnsHashAndUsesBearer()
    {
        var api = new MetaMediaApi(new HttpClient(new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.Query.Should().NotContain("token");
            request.RequestUri.AbsolutePath.Should().EndWith("/act_11/adimages");
            request.Headers.Authorization!.Parameter.Should().Be("token");
            return new(HttpStatusCode.OK) { Content = new StringContent("""{"images":{"file.png":{"hash":"image-hash","url":"https://example.com/image.png"}}}""") };
        })), Options.Create(new MetaAdsOAuthOptions { AppSecret = "secret" }));
        var result = await api.UploadAsync("act_11", "token", "image/png", "file.png", new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0 }, default);
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalId.Should().Be("image-hash");
    }
}
