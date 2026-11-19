using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;
using System.Text;

namespace SocialPublishing.Application.Planning.Copy;

/// <summary>
/// Brief determinístico enviado ao provedor de IA para geração de cópia.
/// </summary>
public sealed class CopyBrief
{
    /// <summary>
    /// Inicializa o brief de cópia.
    /// </summary>
    /// <param name="persona">Persona da marca.</param>
    /// <param name="tone">Tom de voz.</param>
    /// <param name="forbiddenTerms">Termos proibidos.</param>
    /// <param name="mandatoryLinks">Links obrigatórios.</param>
    /// <param name="emojiPolicy">Política de emoji.</param>
    /// <param name="languageCode">Código de idioma.</param>
    /// <param name="hashtagLimit">Teto de hashtags do perfil.</param>
    /// <param name="maxCaptionLength">Teto de caracteres da legenda na rede.</param>
    /// <param name="pillar">Pilar editorial.</param>
    /// <param name="platform">Rede de publicação.</param>
    /// <param name="format">Formato de publicação.</param>
    /// <param name="formatInstruction">Instrução específica de formato.</param>
    /// <param name="businessSummary">Resumo do negócio do cliente.</param>
    /// <param name="targetAudience">Público-alvo.</param>
    /// <param name="mainOffer">Oferta principal.</param>
    /// <param name="differentiators">Diferenciais competitivos.</param>
    /// <param name="primaryColor">Cor primária da identidade visual.</param>
    /// <param name="secondaryColor">Cor secundária.</param>
    /// <param name="accentColor">Cor de destaque.</param>
    public CopyBrief(
        string persona,
        string tone,
        IReadOnlyList<string> forbiddenTerms,
        IReadOnlyList<string> mandatoryLinks,
        EmojiPolicy emojiPolicy,
        string languageCode,
        int hashtagLimit,
        int maxCaptionLength,
        ContentPillar pillar,
        OrganicPlatform platform,
        OrganicPostFormat format,
        string formatInstruction,
        string? businessSummary = null,
        string? targetAudience = null,
        string? mainOffer = null,
        string? differentiators = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? accentColor = null)
    {
        Persona = persona;
        Tone = tone;
        ForbiddenTerms = forbiddenTerms;
        MandatoryLinks = mandatoryLinks;
        EmojiPolicy = emojiPolicy;
        LanguageCode = languageCode;
        HashtagLimit = hashtagLimit;
        MaxCaptionLength = maxCaptionLength;
        Pillar = pillar;
        Platform = platform;
        Format = format;
        FormatInstruction = formatInstruction;
        BusinessSummary = businessSummary;
        TargetAudience = targetAudience;
        MainOffer = mainOffer;
        Differentiators = differentiators;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        AccentColor = accentColor;
    }

    /// <summary>
    /// Obtém a persona da marca.
    /// </summary>
    public string Persona { get; }

    /// <summary>
    /// Obtém o tom de voz.
    /// </summary>
    public string Tone { get; }

    /// <summary>
    /// Obtém os termos proibidos.
    /// </summary>
    public IReadOnlyList<string> ForbiddenTerms { get; }

    /// <summary>
    /// Obtém os links obrigatórios.
    /// </summary>
    public IReadOnlyList<string> MandatoryLinks { get; }

    /// <summary>
    /// Obtém a política de emoji.
    /// </summary>
    public EmojiPolicy EmojiPolicy { get; }

    /// <summary>
    /// Obtém o código de idioma.
    /// </summary>
    public string LanguageCode { get; }

    /// <summary>
    /// Obtém o teto de hashtags do perfil.
    /// </summary>
    public int HashtagLimit { get; }

    /// <summary>
    /// Obtém o teto de caracteres da legenda na rede.
    /// </summary>
    public int MaxCaptionLength { get; }

    /// <summary>
    /// Obtém o pilar editorial.
    /// </summary>
    public ContentPillar Pillar { get; }

    /// <summary>
    /// Obtém a rede de publicação.
    /// </summary>
    public OrganicPlatform Platform { get; }

    /// <summary>
    /// Obtém o formato de publicação.
    /// </summary>
    public OrganicPostFormat Format { get; }

    /// <summary>
    /// Obtém a instrução específica de formato.
    /// </summary>
    public string FormatInstruction { get; }

    /// <summary>
    /// Obtém o resumo do negócio do cliente, quando informado.
    /// </summary>
    public string? BusinessSummary { get; }

    /// <summary>
    /// Obtém o público-alvo, quando informado.
    /// </summary>
    public string? TargetAudience { get; }

    /// <summary>
    /// Obtém a oferta principal, quando informada.
    /// </summary>
    public string? MainOffer { get; }

    /// <summary>
    /// Obtém os diferenciais competitivos, quando informados.
    /// </summary>
    public string? Differentiators { get; }

    /// <summary>
    /// Obtém a cor primária da identidade visual, quando informada.
    /// </summary>
    public string? PrimaryColor { get; }

    /// <summary>
    /// Obtém a cor secundária, quando informada.
    /// </summary>
    public string? SecondaryColor { get; }

    /// <summary>
    /// Obtém a cor de destaque, quando informada.
    /// </summary>
    public string? AccentColor { get; }

    /// <summary>
    /// Serializa o brief em texto para inspeção e prompt do modelo.
    /// </summary>
    /// <returns>Representação textual do brief.</returns>
    public string ToPromptText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Persona: {Persona}");
        sb.AppendLine($"Tom: {Tone}");
        if (!string.IsNullOrWhiteSpace(BusinessSummary))
        {
            sb.AppendLine($"Negócio: {BusinessSummary}");
        }

        if (!string.IsNullOrWhiteSpace(TargetAudience))
        {
            sb.AppendLine($"Público-alvo: {TargetAudience}");
        }

        if (!string.IsNullOrWhiteSpace(MainOffer))
        {
            sb.AppendLine($"Oferta principal: {MainOffer}");
        }

        if (!string.IsNullOrWhiteSpace(Differentiators))
        {
            sb.AppendLine($"Diferenciais: {Differentiators}");
        }

        AppendVisualIdentityLine(sb);
        sb.AppendLine($"Idioma: {LanguageCode}");
        sb.AppendLine($"Pilar: {Pillar}");
        sb.AppendLine($"Rede: {Platform}");
        sb.AppendLine($"Formato: {Format}");
        sb.AppendLine($"Emoji: {EmojiPolicy}");
        sb.AppendLine($"Teto legenda: {MaxCaptionLength}");
        sb.AppendLine($"Teto hashtags (perfil): {HashtagLimit}");
        sb.AppendLine($"Termos proibidos: {string.Join(", ", ForbiddenTerms)}");
        sb.AppendLine($"Links obrigatórios: {string.Join(", ", MandatoryLinks)}");
        sb.AppendLine($"Instrução de formato: {FormatInstruction}");
        return sb.ToString();
    }

    private void AppendVisualIdentityLine(StringBuilder sb)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(PrimaryColor))
        {
            parts.Add($"primária {PrimaryColor}");
        }

        if (!string.IsNullOrWhiteSpace(SecondaryColor))
        {
            parts.Add($"secundária {SecondaryColor}");
        }

        if (!string.IsNullOrWhiteSpace(AccentColor))
        {
            parts.Add($"destaque {AccentColor}");
        }

        if (parts.Count > 0)
        {
            sb.AppendLine($"Cores da identidade visual: {string.Join("; ", parts)}");
        }
    }
}
