using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="TenantBranding"/> entity and validation invariants.
/// </summary>
public sealed class TenantBrandingTests
{
    private readonly Guid _validId = Guid.NewGuid();
    private const string ValidPrimaryColor = "#4F46E5";
    private const string ValidSecondaryColor = "#0F172A";
    private const string ValidLightLogo = "https://cdn.admetricspro.com/logos/vanguarda-light.png";
    private const string ValidDarkLogo = "https://cdn.admetricspro.com/logos/vanguarda-dark.png";
    private const string ValidFavicon = "https://cdn.admetricspro.com/logos/vanguarda-favicon.ico";

    /// <summary>
    /// Verifies that valid branding parameters instantiate the entity successfully.
    /// </summary>
    [Fact]
    public void Create_WithValidParameters_ShouldReturnSuccess()
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor,
            ValidLightLogo,
            ValidDarkLogo,
            ValidFavicon);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var branding = result.Value;
        branding.Id.Should().Be(_validId);
        branding.PrimaryColor.Should().Be(ValidPrimaryColor);
        branding.SecondaryColor.Should().Be(ValidSecondaryColor);
        branding.LightLogoUrl.Should().Be(ValidLightLogo);
        branding.DarkLogoUrl.Should().Be(ValidDarkLogo);
        branding.FaviconUrl.Should().Be(ValidFavicon);
        branding.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        branding.UpdatedAtUtc.Should().BeNull();
    }

    /// <summary>
    /// Verifies that branding with only required colors (no logo URLs) creates successfully.
    /// </summary>
    [Fact]
    public void Create_WithNullLogoUrls_ShouldReturnSuccess()
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            "#123456",
            "#abcdef");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var branding = result.Value;
        branding.LightLogoUrl.Should().BeNull();
        branding.DarkLogoUrl.Should().BeNull();
        branding.FaviconUrl.Should().BeNull();
    }

    /// <summary>
    /// Verifies that empty Guid returns a validation failure.
    /// </summary>
    [Fact]
    public void Create_WithEmptyId_ShouldReturnFailure()
    {
        // Act
        var result = TenantBranding.Create(
            Guid.Empty,
            ValidPrimaryColor,
            ValidSecondaryColor);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidId");
    }

    /// <summary>
    /// Verifies that invalid hex primary color returns a validation failure.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("blue")]
    [InlineData("#GGGGGG")]
    [InlineData("#12345")]
    [InlineData("#1234567890")]
    public void Create_WithInvalidPrimaryColor_ShouldReturnFailure(string? invalidColor)
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            invalidColor!,
            ValidSecondaryColor);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidPrimaryColor");
    }

    /// <summary>
    /// Verifies that invalid hex secondary color returns a validation failure.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("red")]
    [InlineData("#ZZZZZZ")]
    public void Create_WithInvalidSecondaryColor_ShouldReturnFailure(string? invalidColor)
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            invalidColor!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidSecondaryColor");
    }

    /// <summary>
    /// Verifies that an invalid URI for light logo returns a validation failure.
    /// </summary>
    [Fact]
    public void Create_WithInvalidLightLogoUrl_ShouldReturnFailure()
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor,
            lightLogoUrl: "not-a-valid-url");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidLightLogoUrl");
    }

    /// <summary>
    /// Verifies that an invalid URI for dark logo returns a validation failure.
    /// </summary>
    [Fact]
    public void Create_WithInvalidDarkLogoUrl_ShouldReturnFailure()
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor,
            darkLogoUrl: "ftp://invalid-scheme");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidDarkLogoUrl");
    }

    /// <summary>
    /// Verifies that an invalid URI for favicon returns a validation failure.
    /// </summary>
    [Fact]
    public void Create_WithInvalidFaviconUrl_ShouldReturnFailure()
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor,
            faviconUrl: "invalid-favicon");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidFaviconUrl");
    }

    /// <summary>
    /// Verifies that light logo with invalid image file extension returns validation failure.
    /// </summary>
    [Theory]
    [InlineData("https://cdn.example.com/logo.gif")]
    [InlineData("https://cdn.example.com/logo.exe")]
    [InlineData("https://cdn.example.com/logo.pdf")]
    [InlineData("https://cdn.example.com/logo")]
    [InlineData("https://cdn.example.com/logo.bmp")]
    public void Create_WithInvalidLightLogoImageExtension_ShouldReturnFailure(string invalidUrl)
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor,
            lightLogoUrl: invalidUrl);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidLightLogoFormat");
    }

    /// <summary>
    /// Verifies that dark logo with invalid image file extension returns validation failure.
    /// </summary>
    [Theory]
    [InlineData("https://cdn.example.com/dark.gif")]
    [InlineData("https://cdn.example.com/dark.txt")]
    [InlineData("https://cdn.example.com/dark.html")]
    [InlineData("https://cdn.example.com/dark-logo")]
    public void Create_WithInvalidDarkLogoImageExtension_ShouldReturnFailure(string invalidUrl)
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor,
            darkLogoUrl: invalidUrl);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidDarkLogoFormat");
    }

    /// <summary>
    /// Verifies that favicon with invalid file extension returns validation failure.
    /// Favicon must only accept .ico, .png or .svg.
    /// </summary>
    [Theory]
    [InlineData("https://cdn.example.com/favicon.jpg")]
    [InlineData("https://cdn.example.com/favicon.jpeg")]
    [InlineData("https://cdn.example.com/favicon.gif")]
    [InlineData("https://cdn.example.com/favicon.webp")]
    [InlineData("https://cdn.example.com/favicon")]
    public void Create_WithInvalidFaviconExtension_ShouldReturnFailure(string invalidUrl)
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor,
            faviconUrl: invalidUrl);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidFaviconFormat");
    }

    /// <summary>
    /// Verifies that valid image extensions are accepted for logos and favicons.
    /// </summary>
    [Theory]
    [InlineData("https://cdn.example.com/logo.png", "https://cdn.example.com/logo.svg", "https://cdn.example.com/favicon.ico")]
    [InlineData("https://cdn.example.com/logo.jpg", "https://cdn.example.com/logo.jpeg", "https://cdn.example.com/favicon.png")]
    [InlineData("https://cdn.example.com/logo.webp", "https://cdn.example.com/logo.webp", "https://cdn.example.com/favicon.svg")]
    [InlineData("https://cdn.example.com/logo.PNG?version=1", "https://cdn.example.com/logo.SVG#hash", "https://cdn.example.com/favicon.ICO")]
    public void Create_WithValidImageExtensions_ShouldReturnSuccess(string lightLogo, string darkLogo, string favicon)
    {
        // Act
        var result = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor,
            lightLogoUrl: lightLogo,
            darkLogoUrl: darkLogo,
            faviconUrl: favicon);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LightLogoUrl.Should().Be(lightLogo.Trim());
        result.Value.DarkLogoUrl.Should().Be(darkLogo.Trim());
        result.Value.FaviconUrl.Should().Be(favicon.Trim());
    }

    /// <summary>
    /// Verifies that updating colors modifies the fields and sets UpdatedAtUtc.
    /// </summary>
    [Fact]
    public void UpdateColors_WithValidHex_ShouldUpdateColorsAndTimestamp()
    {
        // Arrange
        var branding = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor).Value;

        // Act
        var result = branding.UpdateColors("#00FF00", "#112233");

        // Assert
        result.IsSuccess.Should().BeTrue();
        branding.PrimaryColor.Should().Be("#00FF00");
        branding.SecondaryColor.Should().Be("#112233");
        branding.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that updating logos modifies URLs and sets UpdatedAtUtc.
    /// </summary>
    [Fact]
    public void UpdateLogos_WithValidUrls_ShouldUpdateUrlsAndTimestamp()
    {
        // Arrange
        var branding = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor).Value;

        const string newLight = "https://cdn.example.com/logo-new.png";
        const string newDark = "https://cdn.example.com/dark-new.png";
        const string newFav = "https://cdn.example.com/fav-new.ico";

        // Act
        var result = branding.UpdateLogos(newLight, newDark, newFav);

        // Assert
        result.IsSuccess.Should().BeTrue();
        branding.LightLogoUrl.Should().Be(newLight);
        branding.DarkLogoUrl.Should().Be(newDark);
        branding.FaviconUrl.Should().Be(newFav);
        branding.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that UpdateLogos rejects invalid file extensions.
    /// </summary>
    [Fact]
    public void UpdateLogos_WithInvalidFileExtensions_ShouldReturnFailure()
    {
        // Arrange
        var branding = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor).Value;

        // Act
        var result = branding.UpdateLogos("https://cdn.example.com/logo.gif", null, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidLightLogoFormat");
    }

    /// <summary>
    /// Verifies that UpdateDetails modifies both colors and logos atomically.
    /// </summary>
    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateAllFieldsAndTimestamp()
    {
        // Arrange
        var branding = TenantBranding.Create(
            _validId,
            ValidPrimaryColor,
            ValidSecondaryColor).Value;

        const string newPrimary = "#10B981";
        const string newSecondary = "#1E293B";
        const string newLight = "https://cdn.example.com/brand-light.svg";
        const string newDark = "https://cdn.example.com/brand-dark.svg";
        const string newFav = "https://cdn.example.com/brand-fav.png";

        // Act
        var result = branding.UpdateDetails(newPrimary, newSecondary, newLight, newDark, newFav);

        // Assert
        result.IsSuccess.Should().BeTrue();
        branding.PrimaryColor.Should().Be(newPrimary);
        branding.SecondaryColor.Should().Be(newSecondary);
        branding.LightLogoUrl.Should().Be(newLight);
        branding.DarkLogoUrl.Should().Be(newDark);
        branding.FaviconUrl.Should().Be(newFav);
        branding.UpdatedAtUtc.Should().NotBeNull();
    }
}

