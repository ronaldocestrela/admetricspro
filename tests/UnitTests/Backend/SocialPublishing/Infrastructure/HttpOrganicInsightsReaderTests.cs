using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Insights;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Infrastructure.Insights;
using UnitTests.Backend.Common;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Testes do adaptador HTTP de insights orgânicos (Subfase 6.1).
/// </summary>
public sealed class HttpOrganicInsightsReaderTests
{
    private const string TestAccessToken = "test-access-token-secret-42";
    private const string MediaId = "17841400000000000";
    private const string PagePostId = "page-post-99";
    private const string VideoId = "video-open-123";

    /// <summary>
    /// Instagram: GET insights com métricas de engajamento e token Bearer.
    /// </summary>
    [Fact]
    public async Task Instagram_ReadAsync_ConsultaInsightsComBearer()
    {
        HttpRequestMessage? captured = null;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            captured = req;
            return Json(HttpStatusCode.OK, InstagramInsightsJson());
        });

        var reader = CreateReader(handler);
        var request = new OrganicInsightsReadRequest(
            TestAccessToken,
            "ig-user-1",
            MediaId,
            OrganicPlatform.Instagram);

        var result = await reader.ReadAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Impressions.Should().Be(5000);
        result.Value.Likes.Should().Be(120);
        result.Value.Comments.Should().Be(8);
        result.Value.Shares.Should().Be(3);
        result.Value.Saves.Should().Be(15);

        captured!.Method.Should().Be(HttpMethod.Get);
        captured.RequestUri!.AbsolutePath.Should().Contain($"/{MediaId}/insights");
        captured.RequestUri.Query.Should().Contain("metric=");
        captured.RequestUri.OriginalString.Should().NotContain(TestAccessToken);
        captured.Headers.Authorization!.Scheme.Should().Be("Bearer");
        captured.Headers.Authorization.Parameter.Should().Be(TestAccessToken);
    }

    /// <summary>
    /// Facebook Page: métricas de post e salvamentos zero quando ausentes.
    /// </summary>
    [Fact]
    public async Task FacebookPage_ReadAsync_MapeiaMetricasDePost()
    {
        HttpRequestMessage? captured = null;
        var handler = new TestHttpMessageHandler((req, _) =>
        {
            captured = req;
            return Json(HttpStatusCode.OK, FacebookInsightsJson());
        });

        var reader = CreateReader(handler);
        var request = new OrganicInsightsReadRequest(
            TestAccessToken,
            "page-123",
            PagePostId,
            OrganicPlatform.FacebookPage);

        var result = await reader.ReadAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Impressions.Should().Be(8000);
        result.Value.Likes.Should().Be(200);
        result.Value.Comments.Should().Be(12);
        result.Value.Shares.Should().Be(4);
        result.Value.Saves.Should().Be(0);

        captured!.RequestUri!.AbsolutePath.Should().Contain($"/{PagePostId}/insights");
        captured.Headers.Authorization.Should().NotBeNull();
    }

    /// <summary>
    /// TikTok: POST video/query com Bearer e salvamentos zero.
    /// </summary>
    [Fact]
    public async Task TikTok_ReadAsync_ConsultaVideoQuery()
    {
        HttpRequestMessage? captured = null;
        string? body = null;
        var handler = new TestHttpMessageHandler(async (req, _) =>
        {
            captured = req;
            body = req.Content is null ? null : await req.Content.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, TikTokVideoQueryJson());
        });

        var reader = CreateReader(handler);
        var request = new OrganicInsightsReadRequest(
            TestAccessToken,
            "open-id-1",
            VideoId,
            OrganicPlatform.TikTok);

        var result = await reader.ReadAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Impressions.Should().Be(12000);
        result.Value.Likes.Should().Be(900);
        result.Value.Comments.Should().Be(40);
        result.Value.Shares.Should().Be(22);
        result.Value.Saves.Should().Be(0);

        captured!.Method.Should().Be(HttpMethod.Post);
        captured.RequestUri!.AbsoluteUri.Should().Be("https://open.tiktokapis.com/v2/video/query/");
        body.Should().Contain(VideoId);
        captured.Headers.Authorization!.Parameter.Should().Be(TestAccessToken);
        captured.RequestUri.OriginalString.Should().NotContain(TestAccessToken);
    }

    /// <summary>
    /// HTTP 401 deve mapear para token revogado.
    /// </summary>
    [Fact]
    public async Task ReadAsync_Quando401_RetornaTokenRevoked()
    {
        var handler = new TestHttpMessageHandler((_, _) => Json(HttpStatusCode.Unauthorized, """{"error":"auth"}"""));
        var reader = CreateReader(handler);

        var result = await reader.ReadAsync(SampleInstagramRequest(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.TokenRevoked.Code);
    }

    /// <summary>
    /// Resposta inválida deve retornar InsightsReadFailed.
    /// </summary>
    [Fact]
    public async Task ReadAsync_QuandoCorpoInvalido_RetornaInsightsReadFailed()
    {
        var handler = new TestHttpMessageHandler((_, _) => Json(HttpStatusCode.OK, """{"unexpected":true}"""));
        var reader = CreateReader(handler);

        var result = await reader.ReadAsync(SampleInstagramRequest(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.InsightsReadFailed.Code);
    }

    /// <summary>
    /// Métrica obrigatória ausente deve retornar InsightsReadFailed.
    /// </summary>
    [Fact]
    public async Task ReadAsync_QuandoImpressoesAusentes_RetornaInsightsReadFailed()
    {
        var handler = new TestHttpMessageHandler((_, _) => Json(HttpStatusCode.OK, """{"data":[{"name":"likes","values":[{"value":1}]}]}"""));
        var reader = CreateReader(handler);

        var result = await reader.ReadAsync(SampleInstagramRequest(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.InsightsReadFailed.Code);
    }

    /// <summary>
    /// Cancelamento deve propagar OperationCanceledException.
    /// </summary>
    [Fact]
    public async Task ReadAsync_QuandoCancelado_PropagaOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new TestHttpMessageHandler((_, token) =>
        {
            token.ThrowIfCancellationRequested();
            return Json(HttpStatusCode.OK, InstagramInsightsJson());
        });
        var reader = CreateReader(handler);

        var act = () => reader.ReadAsync(SampleInstagramRequest(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// O leitor não depende de repositório nem de tipos de sugestão de boost.
    /// </summary>
    [Fact]
    public void HttpOrganicInsightsReader_NaoReferenciaRepositorioOuSugestao()
    {
        var ctor = typeof(HttpOrganicInsightsReader).GetConstructors().Single();
        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToArray();
        paramTypes.Should().BeEquivalentTo([typeof(HttpClient), typeof(IOptions<OrganicOAuthOptions>)]);

        typeof(HttpOrganicInsightsReader).Assembly.GetTypes()
            .Where(t => t.FullName?.Contains("HttpOrganicInsightsReader", StringComparison.Ordinal) == true)
            .Should().NotContain(t => t.Name.Contains("PaidBoost", StringComparison.Ordinal));
    }

    private static OrganicInsightsReadRequest SampleInstagramRequest() =>
        new(TestAccessToken, "ig-user-1", MediaId, OrganicPlatform.Instagram);

    private static HttpOrganicInsightsReader CreateReader(HttpMessageHandler handler) =>
        new(new HttpClient(handler), Options.Create(DefaultOAuthOptions()));

    private static OrganicOAuthOptions DefaultOAuthOptions() => new()
    {
        Meta = { GraphApiVersion = "v26.0", AppId = "app" },
        TikTok = { ClientKey = "key", ClientSecret = "secret" },
    };

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string InstagramInsightsJson() =>
        """
        {
          "data": [
            { "name": "impressions", "values": [{ "value": 5000 }] },
            { "name": "likes", "values": [{ "value": 120 }] },
            { "name": "comments", "values": [{ "value": 8 }] },
            { "name": "shares", "values": [{ "value": 3 }] },
            { "name": "saved", "values": [{ "value": 15 }] }
          ]
        }
        """;

    private static string FacebookInsightsJson() =>
        """
        {
          "data": [
            { "name": "post_impressions", "values": [{ "value": 8000 }] },
            { "name": "post_reactions_like_total", "values": [{ "value": 200 }] },
            { "name": "post_comments", "values": [{ "value": 12 }] },
            { "name": "post_shares", "values": [{ "value": 4 }] }
          ]
        }
        """;

    private static string TikTokVideoQueryJson() =>
        """
        {
          "data": {
            "videos": [
              {
                "id": "video-open-123",
                "view_count": 12000,
                "like_count": 900,
                "comment_count": 40,
                "share_count": 22
              }
            ]
          }
        }
        """;
}
