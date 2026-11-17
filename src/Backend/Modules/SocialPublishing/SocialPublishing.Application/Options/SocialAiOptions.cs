namespace SocialPublishing.Application.Options;

/// <summary>
/// Opções do provedor de IA para geração de legendas orgânicas.
/// </summary>
public sealed class SocialAiOptions
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string SectionName = "SocialAi";

    /// <summary>
    /// URL base da API compatível com chat completions (sem barra final).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Identificador do modelo.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Chave de API do provedor.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Temperatura de amostragem.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Teto de tokens na resposta.
    /// </summary>
    public int MaxTokens { get; set; } = 800;

    /// <summary>
    /// Timeout HTTP em segundos.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}
