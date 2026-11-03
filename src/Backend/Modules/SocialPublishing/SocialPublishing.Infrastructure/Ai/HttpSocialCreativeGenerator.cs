using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Planning.Copy;
using SocialPublishing.Domain;

namespace SocialPublishing.Infrastructure.Ai;

/// <summary>
/// Adaptador HTTP compatível com OpenAI chat completions para geração de cópia social.
/// </summary>
public sealed class HttpSocialCreativeGenerator : ISocialCreativeGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly SocialAiOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="HttpSocialCreativeGenerator"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP nomeado.</param>
    /// <param name="options">Opções do provedor SocialAi.</param>
    public HttpSocialCreativeGenerator(HttpClient httpClient, IOptions<SocialAiOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<Result<GeneratedCopyDraft>> GenerateAsync(CopyBrief brief, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.Model) ||
            string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return Result<GeneratedCopyDraft>.Failure(SocialErrors.AiConfigurationMissing);
        }

        var systemPrompt =
            "Você gera legendas orgânicas para redes sociais. Responda APENAS com JSON válido no formato: " +
            "{\"caption\":\"...\",\"hashtags\":{\"brand\":[],\"niche\":[],\"reach\":[]}}. " +
            "Não inclua markdown nem texto fora do JSON.";

        var userPrompt = brief.ToPromptText() +
                         "\nGere caption e hashtags respeitando teto de legenda, teto de hashtags, termos proibidos e instrução de formato.";

        var payload = new ChatCompletionRequest
        {
            Model = _options.Model,
            Temperature = _options.Temperature,
            MaxTokens = _options.MaxTokens,
            Messages =
            [
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", userPrompt),
            ],
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(payload, options: JsonOptions),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<GeneratedCopyDraft>.Failure(SocialErrors.CopyGenerationFailed);
            }

            var envelope = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
            var content = envelope?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return Result<GeneratedCopyDraft>.Failure(SocialErrors.CopyGenerationFailed);
            }

            var json = StripMarkdownFence(content);
            var parsed = JsonSerializer.Deserialize<ModelCopyPayload>(json, JsonOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Caption) || parsed.Hashtags is null)
            {
                return Result<GeneratedCopyDraft>.Failure(SocialErrors.CopyGenerationFailed);
            }

            var draft = new GeneratedCopyDraft(
                parsed.Caption,
                parsed.Hashtags.Brand ?? [],
                parsed.Hashtags.Niche ?? [],
                parsed.Hashtags.Reach ?? []);

            return Result<GeneratedCopyDraft>.Success(draft);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<GeneratedCopyDraft>.Failure(SocialErrors.CopyGenerationFailed);
        }
        catch (HttpRequestException)
        {
            return Result<GeneratedCopyDraft>.Failure(SocialErrors.CopyGenerationFailed);
        }
        catch (JsonException)
        {
            return Result<GeneratedCopyDraft>.Failure(SocialErrors.CopyGenerationFailed);
        }
    }

    private static string StripMarkdownFence(string content)
    {
        var trimmed = content.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var lines = trimmed.Split('\n');
        if (lines.Length < 2)
        {
            return trimmed;
        }

        var inner = lines.Skip(1);
        if (inner.LastOrDefault()?.Trim() == "```")
        {
            inner = inner.SkipLast(1);
        }

        return string.Join('\n', inner).Trim();
    }

    private sealed class ChatCompletionRequest
    {
        public string Model { get; set; } = string.Empty;

        public double Temperature { get; set; }

        public int MaxTokens { get; set; }

        public IReadOnlyList<ChatMessage> Messages { get; set; } = [];
    }

    private sealed record ChatMessage(string Role, string Content);

    private sealed class ChatCompletionResponse
    {
        public IReadOnlyList<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        public ChatChoiceMessage? Message { get; set; }
    }

    private sealed class ChatChoiceMessage
    {
        public string? Content { get; set; }
    }

    private sealed class ModelCopyPayload
    {
        public string? Caption { get; set; }

        public ModelHashtagPayload? Hashtags { get; set; }
    }

    private sealed class ModelHashtagPayload
    {
        public IReadOnlyList<string>? Brand { get; set; }

        public IReadOnlyList<string>? Niche { get; set; }

        public IReadOnlyList<string>? Reach { get; set; }
    }
}
