using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Brand;

/// <summary>
/// Value object com o contexto de negócio do cliente para geração de criativos.
/// </summary>
public sealed class BrandBusinessProfile
{
    /// <summary>
    /// Limite de caracteres do resumo do negócio.
    /// </summary>
    public const int MaxBusinessSummaryLength = 2000;

    /// <summary>
    /// Limite de caracteres do público-alvo.
    /// </summary>
    public const int MaxTargetAudienceLength = 1000;

    /// <summary>
    /// Limite de caracteres da oferta principal.
    /// </summary>
    public const int MaxMainOfferLength = 1000;

    /// <summary>
    /// Limite de caracteres dos diferenciais.
    /// </summary>
    public const int MaxDifferentiatorsLength = 2000;

    private BrandBusinessProfile(
        string? businessSummary,
        string? targetAudience,
        string? mainOffer,
        string? differentiators)
    {
        BusinessSummary = businessSummary;
        TargetAudience = targetAudience;
        MainOffer = mainOffer;
        Differentiators = differentiators;
    }

    /// <summary>
    /// Obtém o resumo do negócio do cliente.
    /// </summary>
    public string? BusinessSummary { get; }

    /// <summary>
    /// Obtém a descrição do público-alvo.
    /// </summary>
    public string? TargetAudience { get; }

    /// <summary>
    /// Obtém a oferta principal comercializada.
    /// </summary>
    public string? MainOffer { get; }

    /// <summary>
    /// Obtém os diferenciais competitivos.
    /// </summary>
    public string? Differentiators { get; }

    /// <summary>
    /// Indica se todos os campos estão ausentes.
    /// </summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(BusinessSummary)
        && string.IsNullOrWhiteSpace(TargetAudience)
        && string.IsNullOrWhiteSpace(MainOffer)
        && string.IsNullOrWhiteSpace(Differentiators);

    /// <summary>
    /// Cria o perfil de negócio a partir de entradas opcionais.
    /// </summary>
    /// <param name="businessSummary">Resumo do negócio.</param>
    /// <param name="targetAudience">Público-alvo.</param>
    /// <param name="mainOffer">Oferta principal.</param>
    /// <param name="differentiators">Diferenciais.</param>
    /// <returns>Perfil normalizado ou <c>null</c> quando todos os campos estão vazios.</returns>
    public static Result<BrandBusinessProfile?> Create(
        string? businessSummary,
        string? targetAudience,
        string? mainOffer,
        string? differentiators)
    {
        var summary = NormalizeOptional(businessSummary);
        var audience = NormalizeOptional(targetAudience);
        var offer = NormalizeOptional(mainOffer);
        var diff = NormalizeOptional(differentiators);

        if (summary is { Length: > MaxBusinessSummaryLength }
            || audience is { Length: > MaxTargetAudienceLength }
            || offer is { Length: > MaxMainOfferLength }
            || diff is { Length: > MaxDifferentiatorsLength })
        {
            return Result<BrandBusinessProfile?>.Failure(SocialErrors.BrandBusinessInfoTooLong);
        }

        if (summary is null && audience is null && offer is null && diff is null)
        {
            return Result<BrandBusinessProfile?>.Success(null);
        }

        return Result<BrandBusinessProfile?>.Success(new BrandBusinessProfile(summary, audience, offer, diff));
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}