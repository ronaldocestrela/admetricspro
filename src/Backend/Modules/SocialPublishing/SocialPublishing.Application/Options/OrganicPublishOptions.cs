namespace SocialPublishing.Application.Options;

/// <summary>
/// Opções de retentativa de publicação orgânica (worker Subfase 4.4).
/// </summary>
public sealed class OrganicPublishOptions
{
    /// <summary>
    /// Nome da seção em configuração.
    /// </summary>
    public const string SectionName = "SocialPublishing:Publish";

    /// <summary>
    /// Quantidade máxima de tentativas de publicação por job (padrão 3).
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Atraso base em milissegundos para backoff exponencial (padrão 500).
    /// </summary>
    public int BaseDelayMs { get; set; } = 500;

    /// <summary>
    /// Teto de atraso em milissegundos (padrão 10 segundos).
    /// </summary>
    public int MaxDelayMs { get; set; } = 10_000;

    /// <summary>
    /// Nível de privacidade padrão para publicação TikTok Content Posting.
    /// </summary>
    public string TikTokPrivacyLevel { get; set; } = "PUBLIC_TO_EVERYONE";
}
