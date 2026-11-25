namespace SocialPublishing.Application.Options;

/// <summary>
/// Opções do provedor Gemini Nano Banana para geração de imagens orgânicas.
/// </summary>
public sealed class SocialImageAiOptions
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string SectionName = "SocialImageAi";

    /// <summary>
    /// URL base da API Gemini (sem barra final).
    /// </summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    /// <summary>
    /// Identificador do modelo de imagem.
    /// </summary>
    public string Model { get; set; } = "gemini-nano-banana-2.1";

    /// <summary>
    /// Chave de API do provedor.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Resolução de saída (1K, 2K ou 4K).
    /// </summary>
    public string ImageSize { get; set; } = "1K";

    /// <summary>
    /// Timeout HTTP em segundos.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 90;
}