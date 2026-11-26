using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly ILogger<HttpGeminiImageGenerator> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="HttpGeminiImageGenerator"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP nomeado.</param>
    /// <param name="options">Opções do provedor de imagem.</param>
    /// <param name="logger">Logger estruturado para falhas de parse da resposta.</param>
    public HttpGeminiImageGenerator(
        HttpClient httpClient,
        IOptions<SocialImageAiOptions> options,
        ILogger<HttpGeminiImageGenerator>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<HttpGeminiImageGenerator>.Instance;
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

        var promptText = brief.ToPromptText();
        var payload = new InteractionRequest
        {
            Model = _options.Model,
            Input = brief.HasLogoReference
                ? new object[]
                {
                    new InteractionInputText { Text = promptText },
                    new InteractionInputImage
                    {
                        Data = Convert.ToBase64String(brief.LogoReferenceContent!),
                        MimeType = brief.LogoReferenceMimeType!,
                    },
                }
                : promptText,
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
                LogUnrecognizedInteractionShape(json);
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

    /// <summary>
    /// Extrai bytes de imagem do JSON da Interactions API (formatos legados e REST).
    /// </summary>
    /// <param name="json">Corpo JSON da resposta HTTP.</param>
    /// <returns>Bytes decodificados ou falha de negócio.</returns>
    private Result<byte[]> ExtractImageBytes(string json)
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

                if (TryExtractLastModelOutputImage(steps, out var fromModelOutput))
                {
                    return Result<byte[]>.Success(fromModelOutput);
                }

                if (TryExtractLastThoughtSummaryImage(steps, out var fromThought))
                {
                    return Result<byte[]>.Success(fromThought);
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

    /// <summary>
    /// Registra o formato da resposta quando nenhum bloco de imagem foi localizado (sem dados sensíveis).
    /// </summary>
    /// <param name="json">Corpo JSON da resposta HTTP.</param>
    private void LogUnrecognizedInteractionShape(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var status = root.TryGetProperty("status", out var statusEl)
                ? statusEl.GetString()
                : null;

            var stepTypes = new List<string>();
            var contentTypes = new List<string>();

            if (root.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
            {
                foreach (var step in steps.EnumerateArray())
                {
                    if (step.TryGetProperty("type", out var stepType))
                    {
                        stepTypes.Add(stepType.GetString() ?? "?");
                    }

                    if (step.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var block in content.EnumerateArray())
                        {
                            if (block.TryGetProperty("type", out var blockType))
                            {
                                contentTypes.Add(blockType.GetString() ?? "?");
                            }
                        }
                    }

                    if (step.TryGetProperty("summary", out var summary) && summary.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var block in summary.EnumerateArray())
                        {
                            if (block.TryGetProperty("type", out var blockType))
                            {
                                contentTypes.Add("summary:" + (blockType.GetString() ?? "?"));
                            }
                        }
                    }
                }
            }

            _logger.LogWarning(
                "Gemini Interactions response sem imagem extraível. Status={Status} StepTypes={StepTypes} ContentTypes={ContentTypes}",
                status,
                string.Join(',', stepTypes),
                string.Join(',', contentTypes));
        }
        catch (JsonException)
        {
            _logger.LogWarning("Gemini Interactions response sem imagem extraível; corpo JSON não pôde ser inspecionado.");
        }
    }

    private static bool TryExtractLastModelOutputImage(JsonElement steps, out byte[] bytes)
    {
        bytes = [];
        byte[]? last = null;

        foreach (var step in steps.EnumerateArray())
        {
            if (!step.TryGetProperty("type", out var stepType) ||
                !string.Equals(stepType.GetString(), "model_output", StringComparison.Ordinal))
            {
                continue;
            }

            if (!step.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var block in content.EnumerateArray())
            {
                if (TryReadImageContentBlock(block, out var decoded))
                {
                    last = decoded;
                }
            }
        }

        if (last is null)
        {
            return false;
        }

        bytes = last;
        return true;
    }

    private static bool TryExtractLastThoughtSummaryImage(JsonElement steps, out byte[] bytes)
    {
        bytes = [];
        byte[]? last = null;

        foreach (var step in steps.EnumerateArray())
        {
            if (!step.TryGetProperty("type", out var stepType) ||
                !string.Equals(stepType.GetString(), "thought", StringComparison.Ordinal))
            {
                continue;
            }

            if (!step.TryGetProperty("summary", out var summary) || summary.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var block in summary.EnumerateArray())
            {
                if (TryReadImageContentBlock(block, out var decoded))
                {
                    last = decoded;
                }
            }
        }

        if (last is null)
        {
            return false;
        }

        bytes = last;
        return true;
    }

    private static bool TryReadImageContentBlock(JsonElement block, out byte[] bytes)
    {
        bytes = [];
        if (!block.TryGetProperty("type", out var typeEl) ||
            !string.Equals(typeEl.GetString(), "image", StringComparison.Ordinal))
        {
            return false;
        }

        if (!block.TryGetProperty("data", out var dataElement))
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

        public object Input { get; set; } = string.Empty;

        public InteractionResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class InteractionInputText
    {
        public string Type { get; set; } = "text";

        public string Text { get; set; } = string.Empty;
    }

    private sealed class InteractionInputImage
    {
        public string Type { get; set; } = "image";

        public string Data { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;
    }

    private sealed class InteractionResponseFormat
    {
        public string Type { get; set; } = "image";

        public string? AspectRatio { get; set; }

        public string? ImageSize { get; set; }
    }
}