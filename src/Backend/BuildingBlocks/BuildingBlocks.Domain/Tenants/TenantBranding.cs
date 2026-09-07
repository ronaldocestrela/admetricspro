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
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new <see cref="TenantBranding"/> instance validating color formats and optional URLs.
    /// </summary>
    /// <param name="id">The branding configuration identifier.</param>
    /// <param name="primaryColor">Primary brand color in hex format.</param>
    /// <param name="secondaryColor">Secondary brand color in hex format.</param>
    /// <param name="lightLogoUrl">Optional URL for light theme logo.</param>
    /// <param name="darkLogoUrl">Optional URL for dark theme logo.</param>
    /// <param name="faviconUrl">Optional URL for favicon.</param>
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

        var lightLogoResult = ValidateUrl(lightLogoUrl, "TenantBranding.InvalidLightLogoUrl", "TenantBranding.UrlTooLong");
        if (lightLogoResult.IsFailure)
        {
            return Result<TenantBranding>.Failure(lightLogoResult.Error);
        }

        var darkLogoResult = ValidateUrl(darkLogoUrl, "TenantBranding.InvalidDarkLogoUrl", "TenantBranding.UrlTooLong");
        if (darkLogoResult.IsFailure)
        {
            return Result<TenantBranding>.Failure(darkLogoResult.Error);
        }

        var faviconResult = ValidateUrl(faviconUrl, "TenantBranding.InvalidFaviconUrl", "TenantBranding.UrlTooLong");
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
    /// <param name="lightLogoUrl">Optional URL for light theme logo.</param>
    /// <param name="darkLogoUrl">Optional URL for dark theme logo.</param>
    /// <param name="faviconUrl">Optional URL for favicon icon.</param>
    /// <returns>A <see cref="Result"/> indicating success or validation error.</returns>
    public Result UpdateLogos(string? lightLogoUrl, string? darkLogoUrl, string? faviconUrl)
    {
        var lightLogoResult = ValidateUrl(lightLogoUrl, "TenantBranding.InvalidLightLogoUrl", "TenantBranding.UrlTooLong");
        if (lightLogoResult.IsFailure)
        {
            return Result.Failure(lightLogoResult.Error);
        }

        var darkLogoResult = ValidateUrl(darkLogoUrl, "TenantBranding.InvalidDarkLogoUrl", "TenantBranding.UrlTooLong");
        if (darkLogoResult.IsFailure)
        {
            return Result.Failure(darkLogoResult.Error);
        }

        var faviconResult = ValidateUrl(faviconUrl, "TenantBranding.InvalidFaviconUrl", "TenantBranding.UrlTooLong");
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

    private static Result ValidateUrl(string? url, string formatErrorCode, string lengthErrorCode)
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
            return Result.Failure(Error.Validation(formatErrorCode, "Provided URL must be a valid absolute HTTP or HTTPS address."));
        }

        return Result.Success();
    }

    private static string? NormalizeUrl(string? url)
    {
        return string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }
}
