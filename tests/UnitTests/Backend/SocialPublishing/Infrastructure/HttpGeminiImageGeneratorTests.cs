using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Planning.Images;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Infrastructure.Ai;
using UnitTests.Backend.Common;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Infrastructure;

/// <summary>
/// Testes do adaptador HTTP <see cref="HttpGeminiImageGenerator"/>.
/// </summary>
public sealed class HttpGeminiImageGeneratorTests
{
    private const string ApiKey = "gemini-test-key";

    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static readonly byte[] AlternatePng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAADElEQVQI12P4z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    /// <summary>
    /// Resposta válida com output_image deve retornar bytes PNG.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ComOutputImageValido_DeveRetornarPng()
    {
        var handler = new TestHttpMessageHandler((_, _) => SuccessInteractionResponse(MinimalPng));
        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Equal(MinimalPng);
    }

    /// <summary>
    /// HTTP 500 retorna ImageGenerationFailed.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ComHttp500_RetornaImageGenerationFailed()
    {
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.ImageGenerationFailed.Code);
    }

    /// <summary>
    /// Configuração ausente retorna ImageAiConfigurationMissing.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_SemApiKey_RetornaImageAiConfigurationMissing()
    {
        var handler = new TestHttpMessageHandler((_, _) => SuccessInteractionResponse(MinimalPng));
        var generator = CreateGenerator(handler, apiKey: string.Empty);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.ImageAiConfigurationMissing.Code);
    }

    /// <summary>
    /// Resposta com steps.inline_data deve retornar bytes PNG.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ComStepsInlineData_DeveRetornarPng()
    {
        var handler = new TestHttpMessageHandler((_, _) => StepsInlineDataResponse(MinimalPng));
        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Equal(MinimalPng);
    }

    /// <summary>
    /// Resposta REST com steps.model_output.content type image deve retornar bytes PNG.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ComModelOutputContentImage_DeveRetornarPng()
    {
        var handler = new TestHttpMessageHandler((_, _) => ModelOutputContentImageResponse(MinimalPng));
        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Equal(MinimalPng);
    }

    /// <summary>
    /// Com texto e múltiplas imagens em model_output, deve usar a última imagem.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ComVariasImagensEmModelOutput_DeveUsarUltimaImagem()
    {
        var handler = new TestHttpMessageHandler((_, _) => ModelOutputMixedContentResponse(MinimalPng, AlternatePng));
        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Equal(AlternatePng);
    }

    /// <summary>
    /// Resposta com imagem apenas em steps.thought.summary deve retornar bytes PNG.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_ComThoughtSummaryImage_DeveRetornarPng()
    {
        var handler = new TestHttpMessageHandler((_, _) => ThoughtSummaryImageResponse(MinimalPng));
        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Equal(MinimalPng);
    }

    /// <summary>
    /// Corpo sem imagem retorna ImageGenerationFailed.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_SemImagemNoCorpo_RetornaImageGenerationFailed()
    {
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        });

        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(SampleBrief(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.ImageGenerationFailed.Code);
    }

    private static HttpGeminiImageGenerator CreateGenerator(
        TestHttpMessageHandler handler,
        string apiKey = ApiKey) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com/") },
            Options.Create(new SocialImageAiOptions
            {
                ApiKey = apiKey,
                BaseUrl = "https://generativelanguage.googleapis.com",
                Model = "gemini-nano-banana-2.1",
                ImageSize = "1K",
            }));

    private static ImageBrief SampleBrief() =>
        new(
            "Legenda",
            ContentPillar.Authority,
            OrganicPlatform.Instagram,
            "Persona",
            "Tom",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            ImageBriefBuilder.InstagramFeedAspectRatio,
            "1K",
            null);

    private static HttpResponseMessage SuccessInteractionResponse(byte[] png)
    {
        var base64 = Convert.ToBase64String(png);
        var json = JsonSerializer.Serialize(new { output_image = new { data = base64 } });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private static HttpResponseMessage StepsInlineDataResponse(byte[] png)
    {
        var base64 = Convert.ToBase64String(png);
        var json = JsonSerializer.Serialize(new
        {
            steps = new[]
            {
                new { inline_data = new { mime_type = "image/png", data = base64 } },
            },
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private static HttpResponseMessage ModelOutputContentImageResponse(byte[] png)
    {
        var base64 = Convert.ToBase64String(png);
        var json = JsonSerializer.Serialize(new
        {
            status = "completed",
            steps = new[]
            {
                new
                {
                    type = "model_output",
                    content = new[]
                    {
                        new { type = "image", data = base64, mime_type = "image/png" },
                    },
                },
            },
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private static HttpResponseMessage ModelOutputMixedContentResponse(byte[] firstPng, byte[] lastPng)
    {
        var firstBase64 = Convert.ToBase64String(firstPng);
        var lastBase64 = Convert.ToBase64String(lastPng);
        var json = JsonSerializer.Serialize(new
        {
            status = "completed",
            steps = new[]
            {
                new
                {
                    type = "model_output",
                    content = new object[]
                    {
                        new { type = "text", text = "draft" },
                        new { type = "image", data = firstBase64, mime_type = "image/png" },
                        new { type = "text", text = "final" },
                        new { type = "image", data = lastBase64, mime_type = "image/png" },
                    },
                },
            },
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private static HttpResponseMessage ThoughtSummaryImageResponse(byte[] png)
    {
        var base64 = Convert.ToBase64String(png);
        var json = JsonSerializer.Serialize(new
        {
            status = "completed",
            steps = new[]
            {
                new
                {
                    type = "thought",
                    summary = new object[]
                    {
                        new { type = "text", text = "composing" },
                        new { type = "image", data = base64, mime_type = "image/png" },
                    },
                },
            },
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }
}