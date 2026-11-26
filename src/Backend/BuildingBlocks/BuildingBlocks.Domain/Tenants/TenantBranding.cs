using System.Text.RegularExpressions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Domain entity representing white-label branding identity for an operational tenant.
/// </summary>
public sealed partial class TenantBranding : Entity<Guid>
{
    private static readonly Regex HexColorRegex = new(
        @"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
        RegexOptions.Compiled);

    private TenantBranding(
        Guid id,
        string primaryColor,
        string secondaryColor,
        string? lightLogoUrl,
        string? darkLogoUrl,
        string? faviconUrl,
        DateTime createdAtUtc,
        string? lightLogoObjectKey = null,
        string? darkLogoObjectKey = null,
        string? faviconObjectKey = null,
        string? lightLogoContentType = null,
        string? darkLogoContentType = null,
        string? faviconContentType = null)
        : base(id)
    {
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        LightLogoUrl = lightLogoUrl;
        DarkLogoUrl = darkLogoUrl;
        FaviconUrl = faviconUrl;
        LightLogoObjectKey = lightLogoObjectKey;
        DarkLogoObjectKey = darkLogoObjectKey;
        FaviconObjectKey = faviconObjectKey;
        LightLogoContentType = lightLogoContentType;
        DarkLogoContentType = darkLogoContentType;
        FaviconContentType = faviconContentType;
        CreatedAtUtc = createdAtUtc;
    }

    private TenantBranding()
        : base(Guid.Empty)
    {
        PrimaryColor = string.Empty;
        SecondaryColor = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the primary brand color in hexadecimal format (e.g. #4F46E5).
    /// </summary>
    public string PrimaryColor { get; private set; }

    /// <summary>
    /// Gets the secondary brand color in hexadecimal format (e.g. #0F172A).
    /// </summary>
    public string SecondaryColor { get; private set; }

    /// <summary>
    /// Gets the optional URL of the logo optimized for light backgrounds.
    /// </summary>
    public string? LightLogoUrl { get; private set; }

    /// <summary>
    /// Gets the optional URL of the logo optimized for dark backgrounds.
    /// </summary>
    public string? DarkLogoUrl { get; private set; }

    /// <summary>
    /// Gets the optional URL of the browser tab favicon icon.
    /// </summary>
    public string? FaviconUrl { get; private set; }

    /// <summary>
    /// Gets the optional object key for the light logo in the branding bucket.
    /// </summary>
    public string? LightLogoObjectKey { get; private set; }

    /// <summary>
    /// Gets the optional object key for the dark logo in the branding bucket.
    /// </summary>
    public string? DarkLogoObjectKey { get; private set; }

    /// <summary>
    /// Gets the optional object key for the favicon in the branding bucket.
    /// </summary>
    public string? FaviconObjectKey { get; private set; }

    /// <summary>
    /// Gets the MIME type gravado para o logo claro (quando há objeto).
    /// </summary>
    public string? LightLogoContentType { get; private set; }

    /// <summary>
    /// Gets the MIME type gravado para o logo escuro (quando há objeto).
    /// </summary>
    public string? DarkLogoContentType { get; private set; }

    /// <summary>
    /// Gets the MIME type gravado para o favicon (quando há objeto).
    /// </summary>
    public string? FaviconContentType { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the branding configuration was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp of the most recent branding update.
    /// </summary>
    private static readonly HashSet<string> AllowedLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".svg", ".jpg", ".jpeg", ".webp"
    };

    private static readonly HashSet<string> AllowedFaviconExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ico", ".png", ".svg"
    };

    /// <summary>
    /// Gets the UTC timestamp of the most recent branding update.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new <see cref="TenantBranding"/> instance validating color formats, image file extensions, and optional URLs.
    /// </summary>
    /// <param name="id">The branding configuration identifier.</param>
    /// <param name="primaryColor">Primary brand color in hex format.</param>
    /// <param name="secondaryColor">Secondary brand color in hex format.</param>
    /// <param name="lightLogoUrl">Optional URL for light theme logo (.png, .svg, .jpg, .jpeg, .webp).</param>
    /// <param name="darkLogoUrl">Optional URL for dark theme logo (.png, .svg, .jpg, .jpeg, .webp).</param>
    /// <param name="faviconUrl">Optional URL for favicon (.ico, .png, .svg).</param>
    /// <param name="createdAtUtc">Optional creation timestamp; defaults to UTC now.</param>
    /// <returns>A <see cref="Result{T}"/> with the created entity or validation error.</returns>
    public static Result<TenantBranding> Create(
        Guid id,
        string primaryColor,
        string secondaryColor,
        string? lightLogoUrl = null,
        string? darkLogoUrl = null,
        string? faviconUrl = null,
        DateTime? createdAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            return Result<TenantBranding>.Failure(Error.Validation("TenantBranding.InvalidId", "Branding identifier cannot be empty."));
        }

        var primaryResult = ValidateColor(primaryColor, "TenantBranding.InvalidPrimaryColor", "Primary color");
        if (primaryResult.IsFailure)
        {
            return Result<TenantBranding>.Failure(primaryResult.Error);
        }

        var secondaryResult = ValidateColor(secondaryColor, "TenantBranding.InvalidSecondaryColor", "Secondary color");
        if (secondaryResult.IsFailure)
        {
            return Result<TenantBranding>.Failure(secondaryResult.Error);
        }

        var lightLogoResult = ValidateImageUrl(
            lightLogoUrl,
            "TenantBranding.InvalidLightLogoUrl",
            "TenantBranding.UrlTooLong",
            "TenantBranding.InvalidLightLogoFormat",
            AllowedLogoExtensions);
        if (lightLogoResult.IsFailure)
        {
            return Result<TenantBranding>.Failure(lightLogoResult.Error);
        }

        var darkLogoResult = ValidateImageUrl(
            darkLogoUrl,
            "TenantBranding.InvalidDarkLogoUrl",
            "TenantBranding.UrlTooLong",
            "TenantBranding.InvalidDarkLogoFormat",
            AllowedLogoExtensions);
        if (darkLogoResult.IsFailure)
        {
            return Result<TenantBranding>.Failure(darkLogoResult.Error);
        }

        var faviconResult = ValidateImageUrl(
            faviconUrl,
            "TenantBranding.InvalidFaviconUrl",
            "TenantBranding.UrlTooLong",
            "TenantBranding.InvalidFaviconFormat",
            AllowedFaviconExtensions);
        if (faviconResult.IsFailure)
        {
            return Result<TenantBranding>.Failure(faviconResult.Error);
        }

        var branding = new TenantBranding(
            id,
            primaryColor.Trim(),
            secondaryColor.Trim(),
            NormalizeUrl(lightLogoUrl),
            NormalizeUrl(darkLogoUrl),
            NormalizeUrl(faviconUrl),
            createdAtUtc ?? DateTime.UtcNow);

        return Result<TenantBranding>.Success(branding);
    }

    /// <summary>
    /// Updates the primary and secondary brand colors.
    /// </summary>
    /// <param name="primaryColor">The new primary hex color.</param>
    /// <param name="secondaryColor">The new secondary hex color.</param>
    /// <returns>A <see cref="Result"/> indicating success or validation error.</returns>
    public Result UpdateColors(string primaryColor, string secondaryColor)
    {
        var primaryResult = ValidateColor(primaryColor, "TenantBranding.InvalidPrimaryColor", "Primary color");
        if (primaryResult.IsFailure)
        {
            return Result.Failure(primaryResult.Error);
        }

        var secondaryResult = ValidateColor(secondaryColor, "TenantBranding.InvalidSecondaryColor", "Secondary color");
        if (secondaryResult.IsFailure)
        {
            return Result.Failure(secondaryResult.Error);
        }

        PrimaryColor = primaryColor.Trim();
        SecondaryColor = secondaryColor.Trim();
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Updates the logo and favicon URLs for white-label styling.
    /// </summary>
    /// <param name="lightLogoUrl">Optional URL for light theme logo (.png, .svg, .jpg, .jpeg, .webp).</param>
    /// <param name="darkLogoUrl">Optional URL for dark theme logo (.png, .svg, .jpg, .jpeg, .webp).</param>
    /// <param name="faviconUrl">Optional URL for favicon icon (.ico, .png, .svg).</param>
    /// <returns>A <see cref="Result"/> indicating success or validation error.</returns>
    public Result UpdateLogos(string? lightLogoUrl, string? darkLogoUrl, string? faviconUrl)
    {
        var lightLogoResult = ValidateImageUrl(
            lightLogoUrl,
            "TenantBranding.InvalidLightLogoUrl",
            "TenantBranding.UrlTooLong",
            "TenantBranding.InvalidLightLogoFormat",
            AllowedLogoExtensions);
        if (lightLogoResult.IsFailure)
        {
            return Result.Failure(lightLogoResult.Error);
        }

        var darkLogoResult = ValidateImageUrl(
            darkLogoUrl,
            "TenantBranding.InvalidDarkLogoUrl",
            "TenantBranding.UrlTooLong",
            "TenantBranding.InvalidDarkLogoFormat",
            AllowedLogoExtensions);
        if (darkLogoResult.IsFailure)
        {
            return Result.Failure(darkLogoResult.Error);
        }

        var faviconResult = ValidateImageUrl(
            faviconUrl,
            "TenantBranding.InvalidFaviconUrl",
            "TenantBranding.UrlTooLong",
            "TenantBranding.InvalidFaviconFormat",
            AllowedFaviconExtensions);
        if (faviconResult.IsFailure)
        {
            return Result.Failure(faviconResult.Error);
        }

        LightLogoUrl = NormalizeUrl(lightLogoUrl);
        DarkLogoUrl = NormalizeUrl(darkLogoUrl);
        FaviconUrl = NormalizeUrl(faviconUrl);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Updates all branding attributes atomically (colors and logo assets).
    /// </summary>
    /// <param name="primaryColor">Primary hex color.</param>
    /// <param name="secondaryColor">Secondary hex color.</param>
    /// <param name="lightLogoUrl">Optional light logo URL (.png, .svg, .jpg, .jpeg, .webp).</param>
    /// <param name="darkLogoUrl">Optional dark logo URL (.png, .svg, .jpg, .jpeg, .webp).</param>
    /// <param name="faviconUrl">Optional favicon URL (.ico, .png, .svg).</param>
    /// <returns>A <see cref="Result"/> indicating success or validation error.</returns>
    public Result UpdateDetails(
        string primaryColor,
        string secondaryColor,
        string? lightLogoUrl,
        string? darkLogoUrl,
        string? faviconUrl)
    {
        var colorResult = UpdateColors(primaryColor, secondaryColor);
        if (colorResult.IsFailure)
        {
            return colorResult;
        }

        var logoResult = UpdateLogos(lightLogoUrl, darkLogoUrl, faviconUrl);
        if (logoResult.IsFailure)
        {
            return logoResult;
        }

        return Result.Success();
    }

    /// <summary>
    /// Obtém a URL persistida para o slot (externa ou da API).
    /// </summary>
    /// <param name="kind">Slot do ativo.</param>
    /// <returns>URL ou <see langword="null"/>.</returns>
    public string? GetUrlForKind(TenantBrandStoredAssetKind kind) =>
        kind switch
        {
            TenantBrandStoredAssetKind.Light => LightLogoUrl,
            TenantBrandStoredAssetKind.Dark => DarkLogoUrl,
            TenantBrandStoredAssetKind.Favicon => FaviconUrl,
            _ => null,
        };

    /// <summary>
    /// Obtém a chave de objeto armazenada para o slot informado.
    /// </summary>
    /// <param name="kind">Slot do ativo.</param>
    /// <returns>Chave ou <see langword="null"/>.</returns>
    public string? GetStoredObjectKey(TenantBrandStoredAssetKind kind) =>
        kind switch
        {
            TenantBrandStoredAssetKind.Light => LightLogoObjectKey,
            TenantBrandStoredAssetKind.Dark => DarkLogoObjectKey,
            TenantBrandStoredAssetKind.Favicon => FaviconObjectKey,
            _ => null,
        };

    /// <summary>
    /// Obtém o MIME gravado para o slot informado.
    /// </summary>
    /// <param name="kind">Slot do ativo.</param>
    /// <returns>Tipo MIME ou <see langword="null"/>.</returns>
    public string? GetStoredContentType(TenantBrandStoredAssetKind kind) =>
        kind switch
        {
            TenantBrandStoredAssetKind.Light => LightLogoContentType,
            TenantBrandStoredAssetKind.Dark => DarkLogoContentType,
            TenantBrandStoredAssetKind.Favicon => FaviconContentType,
            _ => null,
        };

    /// <summary>
    /// Associa um upload ao slot: persiste chave, MIME e URL pública da API (substitui URL externa no slot).
    /// </summary>
    /// <param name="kind">Slot do ativo.</param>
    /// <param name="objectKey">Chave canônica no bucket.</param>
    /// <param name="contentType">MIME gravado.</param>
    /// <param name="publicApiUrl">URL absoluta do endpoint público da API.</param>
    /// <returns>Sucesso ou validação.</returns>
    public Result AssignUploadedAsset(
        TenantBrandStoredAssetKind kind,
        string objectKey,
        string contentType,
        string publicApiUrl)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return Result.Failure(Error.Validation("TenantBranding.ObjectKeyRequired", "A chave do objeto é obrigatória."));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return Result.Failure(Error.Validation("TenantBranding.ContentTypeRequired", "O tipo MIME é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(publicApiUrl))
        {
            return Result.Failure(Error.Validation("TenantBranding.PublicUrlRequired", "A URL pública da API é obrigatória."));
        }

        switch (kind)
        {
            case TenantBrandStoredAssetKind.Light:
                LightLogoObjectKey = objectKey.Trim();
                LightLogoContentType = contentType.Trim();
                LightLogoUrl = publicApiUrl.Trim();
                break;
            case TenantBrandStoredAssetKind.Dark:
                DarkLogoObjectKey = objectKey.Trim();
                DarkLogoContentType = contentType.Trim();
                DarkLogoUrl = publicApiUrl.Trim();
                break;
            case TenantBrandStoredAssetKind.Favicon:
                FaviconObjectKey = objectKey.Trim();
                FaviconContentType = contentType.Trim();
                FaviconUrl = publicApiUrl.Trim();
                break;
            default:
                return Result.Failure(Error.Validation("TenantBranding.InvalidAssetSlot", "Slot de ativo inválido."));
        }

        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Zera URL, chave e MIME do slot (após remoção do objeto).
    /// </summary>
    /// <param name="kind">Slot do ativo.</param>
    public void ClearStoredAsset(TenantBrandStoredAssetKind kind)
    {
        switch (kind)
        {
            case TenantBrandStoredAssetKind.Light:
                LightLogoObjectKey = null;
                LightLogoContentType = null;
                LightLogoUrl = null;
                break;
            case TenantBrandStoredAssetKind.Dark:
                DarkLogoObjectKey = null;
                DarkLogoContentType = null;
                DarkLogoUrl = null;
                break;
            case TenantBrandStoredAssetKind.Favicon:
                FaviconObjectKey = null;
                FaviconContentType = null;
                FaviconUrl = null;
                break;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza apenas a URL exibida quando o objeto permanece no bucket.
    /// </summary>
    /// <param name="kind">Slot do ativo.</param>
    /// <param name="publicApiUrl">URL pública da API.</param>
    public void SetStoredAssetPublicUrl(TenantBrandStoredAssetKind kind, string publicApiUrl)
    {
        if (string.IsNullOrWhiteSpace(publicApiUrl))
        {
            return;
        }

        switch (kind)
        {
            case TenantBrandStoredAssetKind.Light:
                LightLogoUrl = publicApiUrl.Trim();
                break;
            case TenantBrandStoredAssetKind.Dark:
                DarkLogoUrl = publicApiUrl.Trim();
                break;
            case TenantBrandStoredAssetKind.Favicon:
                FaviconUrl = publicApiUrl.Trim();
                break;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Aplica URL externa no slot e zera metadados de objeto (sem upload).
    /// </summary>
    /// <param name="kind">Slot do ativo.</param>
    /// <param name="externalUrl">URL HTTPS externa ou <see langword="null"/>.</param>
    public void ApplyExternalUrl(TenantBrandStoredAssetKind kind, string? externalUrl)
    {
        var normalized = NormalizeUrl(externalUrl);
        switch (kind)
        {
            case TenantBrandStoredAssetKind.Light:
                LightLogoObjectKey = null;
                LightLogoContentType = null;
                LightLogoUrl = normalized;
                break;
            case TenantBrandStoredAssetKind.Dark:
                DarkLogoObjectKey = null;
                DarkLogoContentType = null;
                DarkLogoUrl = normalized;
                break;
            case TenantBrandStoredAssetKind.Favicon:
                FaviconObjectKey = null;
                FaviconContentType = null;
                FaviconUrl = normalized;
                break;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static Result ValidateColor(string? color, string errorCode, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return Result.Failure(Error.Validation(errorCode, $"{fieldName} is required."));
        }

        var trimmed = color.Trim();
        if (trimmed.Length > 9 || !HexColorRegex.IsMatch(trimmed))
        {
            return Result.Failure(Error.Validation(errorCode, $"{fieldName} must be a valid hex color code (e.g. #4F46E5)."));
        }

        return Result.Success();
    }

    private static Result ValidateImageUrl(
        string? url,
        string urlFormatErrorCode,
        string lengthErrorCode,
        string imageFormatErrorCode,
        HashSet<string> allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result.Success();
        }

        var trimmed = url.Trim();
        if (trimmed.Length > 1000)
        {
            return Result.Failure(Error.Validation(lengthErrorCode, "URL cannot exceed 1000 characters."));
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure(Error.Validation(urlFormatErrorCode, "Provided URL must be a valid absolute HTTP or HTTPS address."));
        }

        var extension = Path.GetExtension(uri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            return Result.Failure(Error.Validation(
                imageFormatErrorCode,
                $"Image file format is invalid. Allowed extensions are: {string.Join(", ", allowedExtensions)}."));
        }

        return Result.Success();
    }

    private static string? NormalizeUrl(string? url)
    {
        return string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }
}
