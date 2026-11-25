using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Planning.Images;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.Ai;

/// <summary>
/// Adaptador HTTP para geração de imagens via Gemini Interactions API (Nano Banana).
/// </summary>
public sealed class HttpGeminiImageGenerator : ISocialImageGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly SocialImageAiOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="HttpGeminiImageGenerator"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP nomeado.</param>
    /// <param name="options">Opções do provedor de imagem.</param>
    public HttpGeminiImageGenerator(HttpClient httpClient, IOptions<SocialImageAiOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<Result<GeneratedSocialImage>> GenerateAsync(ImageBrief brief, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.Model) ||
            string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Result<GeneratedSocialImage>.Failure(SocialErrors.ImageAiConfigurationMissing);
        }

        var payload = new InteractionRequest
        {
            Model = _options.Model,
            Input = brief.ToPromptText(),
            ResponseFormat = new InteractionResponseFormat
            {
                Type = "image",
                AspectRatio = brief.AspectRatio,
                ImageSize = brief.ImageSize,
            },
        };

        var path = $"v1beta/interactions?key={Uri.EscapeDataString(_options.ApiKey)}";
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload, options: JsonOptions),
        };

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<GeneratedSocialImage>.Failure(SocialErrors.ImageGenerationFailed);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var bytesResult = ExtractImageBytes(json);
            if (bytesResult.IsFailure)
            {
                return Result<GeneratedSocialImage>.Failure(bytesResult.Error);
            }

            return Result<GeneratedSocialImage>.Success(
                new GeneratedSocialImage(bytesResult.Value, OrganicMediaMimeTypes.Png));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<GeneratedSocialImage>.Failure(SocialErrors.ImageGenerationFailed);
        }
        catch (HttpRequestException)
        {
            return Result<GeneratedSocialImage>.Failure(SocialErrors.ImageGenerationFailed);
        }
        catch (JsonException)
        {
            return Result<GeneratedSocialImage>.Failure(SocialErrors.ImageGenerationFailed);
        }
    }

    private static Result<byte[]> ExtractImageBytes(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (TryReadBase64(root, "output_image", "data", out var fromOutput))
            {
                return Result<byte[]>.Success(fromOutput);
            }

            if (root.TryGetProperty("outputs", out var outputs) && outputs.ValueKind == JsonValueKind.Array)
            {
                foreach (var output in outputs.EnumerateArray())
                {
                    if (TryReadBase64(output, "image", "data", out var fromOutputs))
                    {
                        return Result<byte[]>.Success(fromOutputs);
                    }
                }
            }

            if (root.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
            {
                foreach (var step in steps.EnumerateArray())
                {
                    if (TryReadBase64(step, "inline_data", "data", out var fromStep))
                    {
                        return Result<byte[]>.Success(fromStep);
                    }

                    if (TryReadBase64(step, "output_image", "data", out fromStep))
                    {
                        return Result<byte[]>.Success(fromStep);
                    }

                    if (step.TryGetProperty("output", out var stepOutput) &&
                        TryReadBase64(stepOutput, "image", "data", out fromStep))
                    {
                        return Result<byte[]>.Success(fromStep);
                    }
                }
            }

            return Result<byte[]>.Failure(SocialErrors.ImageGenerationFailed);
        }
        catch (FormatException)
        {
            return Result<byte[]>.Failure(SocialErrors.ImageGenerationFailed);
        }
        catch (JsonException)
        {
            return Result<byte[]>.Failure(SocialErrors.ImageGenerationFailed);
        }
    }

    private static bool TryReadBase64(JsonElement parent, string objectProperty, string dataProperty, out byte[] bytes)
    {
        bytes = [];
        if (!parent.TryGetProperty(objectProperty, out var imageObject))
        {
            return false;
        }

        if (!imageObject.TryGetProperty(dataProperty, out var dataElement))
        {
            return false;
        }

        var encoded = dataElement.GetString();
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return false;
        }

        bytes = Convert.FromBase64String(encoded);
        return bytes.Length > 0;
    }

    private sealed class InteractionRequest
    {
        public string Model { get; set; } = string.Empty;

        public string Input { get; set; } = string.Empty;

        public InteractionResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class InteractionResponseFormat
    {
        public string Type { get; set; } = "image";

        public string? AspectRatio { get; set; }

        public string? ImageSize { get; set; }
    }
}