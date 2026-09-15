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
        DateTime createdAtUtc)
        : base(id)
    {
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
        LightLogoUrl = lightLogoUrl;
        DarkLogoUrl = darkLogoUrl;
        FaviconUrl = faviconUrl;
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
