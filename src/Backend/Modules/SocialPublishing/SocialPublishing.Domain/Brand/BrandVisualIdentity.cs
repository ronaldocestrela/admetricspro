using System.Text.RegularExpressions;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Brand;

/// <summary>
/// Value object com as cores da identidade visual do cliente.
/// </summary>
public sealed partial class BrandVisualIdentity
{
    private static readonly Regex HexColorRegex = HexColorPattern();

    private BrandVisualIdentity(string? primaryColor, string? secondaryColor, string? accentColor)
    {
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        AccentColor = accentColor;
    }

    /// <summary>
    /// Obtém a cor primária da marca (#RGB, #RRGGBB ou #RRGGBBAA).
    /// </summary>
    public string? PrimaryColor { get; }

    /// <summary>
    /// Obtém a cor secundária da marca.
    /// </summary>
    public string? SecondaryColor { get; }

    /// <summary>
    /// Obtém a cor de destaque da marca.
    /// </summary>
    public string? AccentColor { get; }

    /// <summary>
    /// Indica se todas as cores estão ausentes.
    /// </summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(PrimaryColor)
        && string.IsNullOrWhiteSpace(SecondaryColor)
        && string.IsNullOrWhiteSpace(AccentColor);

    /// <summary>
    /// Cria a identidade visual a partir de cores opcionais.
    /// </summary>
    /// <param name="primaryColor">Cor primária.</param>
    /// <param name="secondaryColor">Cor secundária.</param>
    /// <param name="accentColor">Cor de destaque.</param>
    /// <returns>Identidade normalizada ou <c>null</c> quando todas as cores estão vazias.</returns>
    public static Result<BrandVisualIdentity?> Create(
        string? primaryColor,
        string? secondaryColor,
        string? accentColor)
    {
        if (HasInvalidColor(primaryColor)
            || HasInvalidColor(secondaryColor)
            || HasInvalidColor(accentColor))
        {
            return Result<BrandVisualIdentity?>.Failure(SocialErrors.BrandColorInvalid);
        }

        var primary = NormalizeColor(primaryColor);
        var secondary = NormalizeColor(secondaryColor);
        var accent = NormalizeColor(accentColor);

        if (primary is null && secondary is null && accent is null)
        {
            return Result<BrandVisualIdentity?>.Success(null);
        }

        return Result<BrandVisualIdentity?>.Success(new BrandVisualIdentity(primary, secondary, accent));
    }

    private static bool HasInvalidColor(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        return trimmed.Length > 9 || !HexColorRegex.IsMatch(trimmed);
    }

    private static string? NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    [GeneratedRegex(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled)]
    private static partial Regex HexColorPattern();
}