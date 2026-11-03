using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Publishing;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Infrastructure.OAuth;
using UnitTests.Backend.Common;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Testes de publicação orgânica nos adaptadores Meta e TikTok (Subfase 4.3).
/// </summary>
public sealed class OrganicPublishAdapterTests
{
    private const string TestAccessToken = "test-access-token-secret-42";
    private const string PageId = "page-123";
    private const string IgUserId = "ig-user-456";

    /// <summary>
    /// Facebook Page: uma chamada POST photos e id externo na resposta.
    /// </summary>
    [Fact]
    public async Task FacebookPage_FeedImage_UmaChamadaPhotos_RetornaExternalId()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            captured = req;
            capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Json(HttpStatusCode.OK, """{"id":"photo-ext-99"}""");
        });

        var adapter = CreateFacebookAdapter(handler);
        var request = new OrganicPublishRequest(
            TestAccessToken,
            PageId,
            OrganicPostFormat.FeedImage,
            "Legenda Page",
            [new OrganicPublishMediaItem("https://cdn.example/a.jpg", 0)]);

        var result = await adapter.PublishAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExternalId.Should().Be("photo-ext-99");
        captured!.Method.Should().Be(HttpMethod.Post);
        captured.RequestUri!.AbsolutePath.Should().Contain($"/{PageId}/photos");
        AssertMetaTokenInFormNotUri(captured, capturedBody!);
    }

    /// <summary>
    /// Instagram carrossel: ordem filhos → container CAROUSEL → media_publish.
    /// </summary>
    [Fact]
    public async Task Instagram_FeedCarousel_OrdemCorretaDeRequisicoes()
    {
        var order = new List<string>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (req.Method == HttpMethod.Post && path.EndsWith("/media", StringComparison.Ordinal))
            {
                var body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                if (body.Contains("is_carousel_item=true", StringComparison.Ordinal))
                {
                    order.Add("child");
                    return Json(HttpStatusCode.OK, $$"""{"id":"child-{{order.Count}}"}""");
                }

                if (body.Contains("media_type=CAROUSEL", StringComparison.Ordinal))
                {
                    order.Add("carousel");
                    return Json(HttpStatusCode.OK, """{"id":"carousel-parent"}""");
                }

                order.Add("unknown-post");
            }
            else if (req.Method == HttpMethod.Post && path.EndsWith("/media_publish", StringComparison.Ordinal))
            {
                order.Add("publish");
                return Json(HttpStatusCode.OK, """{"id":"published-carousel-1"}""");
            }

            return Json(HttpStatusCode.BadRequest, """{"error":{"message":"unexpected"}}""");
        });

        var adapter = CreateInstagramAdapter(handler);
        var request = new OrganicPublishRequest(
            TestAccessToken,
            IgUserId,
            OrganicPostFormat.FeedCarousel,
            "Carrossel",
            [
                new OrganicPublishMediaItem("https://cdn.example/1.jpg", 0),
                new OrganicPublishMediaItem("https://cdn.example/2.jpg", 1),
            ]);

        var result = await adapter.PublishAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExternalId.Should().Be("published-carousel-1");
        order.Should().Equal("child", "child", "carousel", "publish");
    }

    /// <summary>
    /// Instagram Reel: container vídeo, status FINISHED, media_publish.
    /// </summary>
    [Fact]
    public async Task Instagram_Reel_VideoContainer_StatusFinished_Publica()
    {
        var step = 0;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            step++;
            var path = req.RequestUri!.AbsolutePath;
            if (step == 1 && req.Method == HttpMethod.Post && path.EndsWith("/media", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """{"id":"reel-container-1"}""");
            }

            if (step == 2 && req.Method == HttpMethod.Get && path.Contains("reel-container-1", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """{"status_code":"FINISHED"}""");
            }

            if (step == 3 && req.Method == HttpMethod.Post && path.EndsWith("/media_publish", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """{"id":"reel-published-77"}""");
            }

            return Json(HttpStatusCode.BadRequest, """{"error":{"message":"step"}}""");
        });

        var adapter = CreateInstagramAdapter(handler);
        var request = new OrganicPublishRequest(
            TestAccessToken,
            IgUserId,
            OrganicPostFormat.Reel,
            "Reel hook",
            [new OrganicPublishMediaItem("https://cdn.example/reel.mp4", 0)]);

        var result = await adapter.PublishAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExternalId.Should().Be("reel-published-77");
    }

    /// <summary>
    /// TikTok: init upload PULL_FROM_URL e status PUBLISH_COMPLETE.
    /// </summary>
    [Fact]
    public async Task TikTok_VideoVertical_InitEStatus_RetornaPublishId()
    {
        var calls = new List<HttpMethod>();
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            calls.Add(req.Method);
            if (req.RequestUri!.AbsolutePath.Contains("video/init", StringComparison.Ordinal))
            {
                req.Headers.Authorization!.Scheme.Should().Be("Bearer");
                req.Headers.Authorization!.Parameter.Should().Be(TestAccessToken);
                return Json(HttpStatusCode.OK, """{"data":{"publish_id":"tt-pub-555"}}""");
            }

            if (req.RequestUri.AbsolutePath.Contains("status/fetch", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """
                    {"data":{"status":"PUBLISH_COMPLETE","publish_id":"tt-pub-555"}}
                    """);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var adapter = CreateTikTokAdapter(handler);
        var request = new OrganicPublishRequest(
            TestAccessToken,
            "open-id-tt",
            OrganicPostFormat.TikTokVideo,
            "TikTok caption",
            [new OrganicPublishMediaItem("https://cdn.example/v.mp4", 0)]);

        var result = await adapter.PublishAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ExternalId.Should().Be("tt-pub-555");
        calls.Should().HaveCount(2);
    }

    /// <summary>
    /// HTTP 429 mapeia para PublishRateLimited com Retry-After quando presente.
    /// </summary>
    [Fact]
    public async Task Meta_Com429_RetornaPublishRateLimitedComRetryAfter()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)429);
            response.Headers.TryAddWithoutValidation("Retry-After", "120");
            return response;
        });

        var adapter = CreateFacebookAdapter(handler);
        var request = SampleFacebookRequest();

        var result = await adapter.PublishAsync(request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<PublishRateLimitedError>();
        var rateLimited = (PublishRateLimitedError)result.Error;
        rateLimited.Code.Should().Be(SocialErrors.PublishRateLimited.Code);
        rateLimited.RetryAfter.Should().Be(TimeSpan.FromSeconds(120));
    }

    /// <summary>
    /// Erro funcional da rede mapeia para PublishFailed sem exceção.
    /// </summary>
    [Fact]
    public async Task Meta_ErroFuncional_RetornaPublishFailed()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            Json(HttpStatusCode.BadRequest, """{"error":{"message":"Invalid parameter","code":100}}"""));

        var adapter = CreateFacebookAdapter(handler);
        var result = await adapter.PublishAsync(SampleFacebookRequest(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PublishFailed.Code);
    }

    /// <summary>
    /// Mesmo id externo em sucesso repetido continua sucesso idempotente.
    /// </summary>
    [Fact]
    public async Task FacebookPage_MesmoExternalIdDuasVezes_AmbosSucesso()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            Json(HttpStatusCode.OK, """{"id":"same-id-1"}"""));

        var adapter = CreateFacebookAdapter(handler);
        var request = SampleFacebookRequest();

        var first = await adapter.PublishAsync(request, CancellationToken.None);
        var second = await adapter.PublishAsync(request, CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value!.ExternalId.Should().Be("same-id-1");
        second.Value!.ExternalId.Should().Be("same-id-1");
    }

    /// <summary>
    /// Formato Reel no adaptador de Page retorna PublishFormatUnsupported sem HTTP.
    /// </summary>
    [Fact]
    public async Task FacebookPage_Reel_RetornaFormatUnsupported_SemHttp()
    {
        var callCount = 0;
        var handler = new TestHttpMessageHandler((_, _) =>
        {
            callCount++;
            return Json(HttpStatusCode.OK, """{"id":"x"}""");
        });

        var adapter = CreateFacebookAdapter(handler);
        var request = new OrganicPublishRequest(
            TestAccessToken,
            PageId,
            OrganicPostFormat.Reel,
            "x",
            [new OrganicPublishMediaItem("https://cdn.example/v.mp4", 0)]);

        var result = await adapter.PublishAsync(request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PublishFormatUnsupported.Code);
        callCount.Should().Be(0);
    }

    /// <summary>
    /// Token de acesso não aparece na URI das requisições Meta.
    /// </summary>
    [Fact]
    public async Task Meta_PublishAsync_NaoColocaTokenNaUri()
    {
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            req.RequestUri!.OriginalString.Should().NotContain(TestAccessToken);
            return Json(HttpStatusCode.OK, """{"id":"ok"}""");
        });

        var adapter = CreateFacebookAdapter(handler);
        await adapter.PublishAsync(SampleFacebookRequest(), CancellationToken.None);
    }

    private static OrganicPublishRequest SampleFacebookRequest() =>
        new(
            TestAccessToken,
            PageId,
            OrganicPostFormat.FeedImage,
            "Cap",
            [new OrganicPublishMediaItem("https://cdn.example/img.jpg", 0)]);

    private static MetaFacebookPageOrganicAdapter CreateFacebookAdapter(HttpMessageHandler handler) =>
        new(new HttpClient(handler), Options.Create(DefaultOAuthOptions()));

    private static MetaInstagramOrganicAdapter CreateInstagramAdapter(HttpMessageHandler handler) =>
        new(new HttpClient(handler), Options.Create(DefaultOAuthOptions()));

    private static TikTokContentPostingAdapter CreateTikTokAdapter(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            Options.Create(DefaultOAuthOptions()),
            Options.Create(new OrganicPublishOptions()));

    private static OrganicOAuthOptions DefaultOAuthOptions() => new()
    {
        Meta = { GraphApiVersion = "v26.0", AppId = "app" },
        TikTok = { ClientKey = "key", ClientSecret = "secret" },
    };

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static void AssertMetaTokenInFormNotUri(HttpRequestMessage request, string body)
    {
        request.RequestUri!.OriginalString.Should().NotContain(TestAccessToken);
        body.Should().Contain("access_token=");
        body.Should().Contain(Uri.EscapeDataString(TestAccessToken));
    }
}
