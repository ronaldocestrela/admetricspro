using System.Text;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Planning.Images;

/// <summary>
/// Brief determinístico para geração de imagem de feed orgânico.
/// </summary>
/// <param name="CaptionBody">Corpo da legenda sem linha de hashtags.</param>
/// <param name="Pillar">Pilar editorial.</param>
/// <param name="Platform">Rede de destino.</param>
/// <param name="Persona">Persona do Brand Voice.</param>
/// <param name="Tone">Tom de voz.</param>
/// <param name="BusinessSummary">Resumo do negócio, se informado.</param>
/// <param name="TargetAudience">Público-alvo, se informado.</param>
/// <param name="MainOffer">Oferta principal, se informada.</param>
/// <param name="Differentiators">Diferenciais, se informados.</param>
/// <param name="PrimaryColor">Cor primária da identidade visual.</param>
/// <param name="SecondaryColor">Cor secundária da identidade visual.</param>
/// <param name="AccentColor">Cor de destaque da identidade visual.</param>
/// <param name="AspectRatio">Proporção solicitada ao modelo (ex.: 4:5).</param>
/// <param name="ImageSize">Resolução solicitada (ex.: 1K).</param>
/// <param name="OptionalDirection">Direcionamento visual opcional da agência.</param>
public sealed record ImageBrief(
    string CaptionBody,
    ContentPillar Pillar,
    OrganicPlatform Platform,
    string Persona,
    string Tone,
    string? BusinessSummary,
    string? TargetAudience,
    string? MainOffer,
    string? Differentiators,
    string? PrimaryColor,
    string? SecondaryColor,
    string? AccentColor,
    string AspectRatio,
    string ImageSize,
    string? OptionalDirection)
{
    /// <summary>
    /// Serializa o brief em texto para o prompt do modelo de imagem.
    /// </summary>
    /// <returns>Texto do prompt.</returns>
    public string ToPromptText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Crie uma arte estática para post de feed em rede social.");
        sb.AppendLine($"Persona da marca: {Persona}");
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

        AppendColorLine(sb);
        sb.AppendLine($"Pilar editorial: {Pillar}");
        sb.AppendLine($"Rede: {Platform}");
        sb.AppendLine($"Ideia da legenda aprovada: {CaptionBody}");
        if (!string.IsNullOrWhiteSpace(OptionalDirection))
        {
            sb.AppendLine($"Direcionamento visual adicional: {OptionalDirection}");
        }

        sb.AppendLine("Não invente logotipo, oferta, preço ou dados ausentes acima.");
        sb.AppendLine("Evite texto pequeno ilegível. Não inclua hashtags na imagem.");
        return sb.ToString();
    }

    private void AppendColorLine(StringBuilder sb)
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