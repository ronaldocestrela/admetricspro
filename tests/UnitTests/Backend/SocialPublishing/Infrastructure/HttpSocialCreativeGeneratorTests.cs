using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Application.Planning.Copy;
using SocialPublishing.Infrastructure.Ai;
using UnitTests.Backend.Common;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Testes do adaptador HTTP <see cref="HttpSocialCreativeGenerator"/>.
/// </summary>
public sealed class HttpSocialCreativeGeneratorTests
{
    private const string ApiKey = "sk-test-key";

    /// <summary>
    /// Temperatura e max_tokens das opções aparecem no corpo; API key só no header.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_DeveEnviarOpcoesNoCorpo_EApiKeySoNoHeader()
    {
        string? capturedBody = null;
        HttpRequestMessage? capturedRequest = null;

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return SuccessChatResponse("""
                {"caption":"Ok","hashtags":{"brand":["a"],"niche":["b"],"reach":["c"]}}
                """);
        });

        var generator = CreateGenerator(handler, temperature: 0.42, maxTokens: 900);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedRequest!.Headers.Authorization!.Parameter.Should().Be(ApiKey);
        capturedBody.Should().NotContain(ApiKey);

        using var doc = JsonDocument.Parse(capturedBody!);
        doc.RootElement.GetProperty("temperature").GetDouble().Should().Be(0.42);
        doc.RootElement.GetProperty("max_tokens").GetInt32().Should().Be(900);
    }

    /// <summary>
    /// HTTP 500 retorna CopyGenerationFailed sem exceção.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ComHttp500_RetornaCopyGenerationFailed()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.CopyGenerationFailed.Code);
    }

    /// <summary>
    /// Timeout de transporte retorna CopyGenerationFailed.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ComTimeout_RetornaCopyGenerationFailed()
    {
        var handler = new TestHttpMessageHandler(async (_, _) =>
        {
            await Task.Yield();
            throw new TaskCanceledException("timeout");
        });

        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.CopyGenerationFailed.Code);
    }

    /// <summary>
    /// JSON inválido no conteúdo retorna CopyGenerationFailed.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ComJsonInvalido_RetornaCopyGenerationFailed()
    {
        var handler = new TestHttpMessageHandler((_, _) => SuccessChatResponse("not-json"));

        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.CopyGenerationFailed.Code);
    }

    /// <summary>
    /// Configuração ausente não dispara HTTP e retorna AiConfigurationMissing.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_SemConfiguracao_RetornaAiConfigurationMissing_SemRequest()
    {
        var callCount = 0;
        var handler = new TestHttpMessageHandler((_, _) =>
        {
            callCount++;
            return SuccessChatResponse("{}");
        });

        var options = Options.Create(new SocialAiOptions
        {
            BaseUrl = "",
            Model = "",
            ApiKey = "",
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") };
        var generator = new HttpSocialCreativeGenerator(client, options);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.AiConfigurationMissing.Code);
        callCount.Should().Be(0);
    }

    private static HttpSocialCreativeGenerator CreateGenerator(
        TestHttpMessageHandler handler,
        double temperature = 0.7,
        int maxTokens = 800)
    {
        var options = Options.Create(new SocialAiOptions
        {
            BaseUrl = "https://api.example.com/v1",
            Model = "gpt-test",
            ApiKey = ApiKey,
            Temperature = temperature,
            MaxTokens = maxTokens,
            TimeoutSeconds = 30,
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/v1/") };
        return new HttpSocialCreativeGenerator(client, options);
    }

    private static CopyBrief SampleBrief() =>
        CopyBriefBuilder.Build(
            BrandVoiceProfile.Create(
                Guid.NewGuid(),
                "Tom",
                "Persona",
                ["https://example.com"],
                [],
                EmojiPolicy.Allowed,
                15,
                25,
                25,
                25,
                25).Value,
            new EditorialSlotCopyInput(
                ContentPillar.Authority,
                OrganicPlatform.Instagram,
                OrganicPostFormat.FeedImage));

    private static HttpResponseMessage SuccessChatResponse(string innerJson)
    {
        var payload = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = innerJson } },
            },
        });

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
    }
}
