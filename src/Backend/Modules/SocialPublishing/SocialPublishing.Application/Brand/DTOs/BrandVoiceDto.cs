using SocialPublishing.Domain.Brand;

namespace SocialPublishing.Application.Brand.DTOs;

/// <summary>
/// Representação HTTP do DNA de marca persistido por workspace.
/// </summary>
/// <param name="Id">Identificador do perfil.</param>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="Tone">Tom de voz.</param>
/// <param name="Persona">Persona da marca.</param>
/// <param name="MandatoryLinks">Links absolutos obrigatórios.</param>
/// <param name="ForbiddenTerms">Termos proibidos normalizados.</param>
/// <param name="EmojiPolicy">Política de emojis.</param>
/// <param name="HashtagLimit">Teto de hashtags por post no perfil.</param>
/// <param name="AuthorityWeight">Peso do pilar Authority.</param>
/// <param name="SaleWeight">Peso do pilar Sale.</param>
/// <param name="ConnectionWeight">Peso do pilar Connection.</param>
/// <param name="SocialProofWeight">Peso do pilar SocialProof.</param>
/// <param name="LanguageCode">Código de idioma (ex.: pt-BR).</param>
/// <param name="BusinessSummary">Resumo do negócio do cliente.</param>
/// <param name="TargetAudience">Público-alvo.</param>
/// <param name="MainOffer">Oferta principal.</param>
/// <param name="Differentiators">Diferenciais competitivos.</param>
/// <param name="PrimaryColor">Cor primária da identidade visual.</param>
/// <param name="SecondaryColor">Cor secundária.</param>
/// <param name="AccentColor">Cor de destaque.</param>
/// <param name="CreatedAtUtc">Carimbo UTC de criação do perfil.</param>
public sealed record BrandVoiceDto(
    Guid Id,
    Guid WorkspaceId,
    string Tone,
    string Persona,
    IReadOnlyList<string> MandatoryLinks,
    IReadOnlyList<string> ForbiddenTerms,
    EmojiPolicy EmojiPolicy,
    int HashtagLimit,
    int AuthorityWeight,
    int SaleWeight,
    int ConnectionWeight,
    int SocialProofWeight,
    string LanguageCode,
    string? BusinessSummary,
    string? TargetAudience,
    string? MainOffer,
    string? Differentiators,
    string? PrimaryColor,
    string? SecondaryColor,
    string? AccentColor,
    DateTime CreatedAtUtc);
