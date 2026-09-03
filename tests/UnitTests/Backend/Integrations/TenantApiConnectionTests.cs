using FluentAssertions;
using Master.Domain.Integrations;

namespace UnitTests.Backend.Integrations;

/// <summary>
/// Unit tests for the <see cref="TenantApiConnection"/> domain entity.
/// Validates tenant OAuth connection lifecycle, token expiration detection (e.g. within 7 days),
/// and revocation/error states.
/// </summary>
public sealed class TenantApiConnectionTests
{
    /// <summary>
    /// Verifies that empty tenant id or account identifier fails validation.
    /// </summary>
    [Theory]
    [InlineData(null, "act_123")]
    [InlineData("", "act_123")]
    [InlineData("   ", "act_123")]
    [InlineData("Tenant Alpha", null)]
    [InlineData("Tenant Alpha", "")]
    [InlineData("Tenant Alpha", "   ")]
    public void Create_ShouldFail_WhenRequiredFieldsAreInvalid(string? tenantName, string? accountId)
    {
        // Act
        var result = TenantApiConnection.Create(
            tenantId: Guid.NewGuid(),
            tenantName: tenantName!,
            platform: AdPlatform.Meta,
            accountIdentifier: accountId!,
            accountName: "Main Ads Account",
            tokenExpiresAtUtc: DateTime.UtcNow.AddDays(30),
            createdAtUtc: DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ApiConnection.InvalidParameters");
    }

    /// <summary>
    /// Verifies that creating a connection with valid parameters defaults to Connected.
    /// </summary>
    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(60);

        // Act
        var result = TenantApiConnection.Create(
            tenantId: tenantId,
            tenantName: "E-Commerce Brasil",
            platform: AdPlatform.Google,
            accountIdentifier: "987-654-3210",
            accountName: "Google Ads Principal",
            tokenExpiresAtUtc: expiresAt,
            createdAtUtc: now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var connection = result.Value;
        connection.TenantId.Should().Be(tenantId);
        connection.TenantName.Should().Be("E-Commerce Brasil");
        connection.Platform.Should().Be(AdPlatform.Google);
        connection.AccountIdentifier.Should().Be("987-654-3210");
        connection.AccountName.Should().Be("Google Ads Principal");
        connection.Status.Should().Be(ApiConnectionStatus.Connected);
        connection.TokenExpiresAtUtc.Should().Be(expiresAt);
        connection.ErrorMessage.Should().BeNull();
    }

    /// <summary>
    /// Verifies that EvaluateExpiration sets status to ExpiringSoon when within warning window (e.g. 7 days).
    /// </summary>
    [Fact]
    public void EvaluateExpiration_ShouldSetExpiringSoon_WhenTokenExpiresWithinWarningWindow()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var connection = TenantApiConnection.Create(
            tenantId: Guid.NewGuid(),
            tenantName: "Alpha Store",
            platform: AdPlatform.Meta,
            accountIdentifier: "act_555",
            accountName: "Meta Ads",
            tokenExpiresAtUtc: now.AddDays(3),
            createdAtUtc: now).Value;

        // Act
        connection.EvaluateExpiration(now, warningWindow: TimeSpan.FromDays(7));

        // Assert
        connection.Status.Should().Be(ApiConnectionStatus.ExpiringSoon);
    }

    /// <summary>
    /// Verifies that EvaluateExpiration sets status to Expired when token expiration is in the past.
    /// </summary>
    [Fact]
    public void EvaluateExpiration_ShouldSetExpired_WhenTokenExpirationIsInThePast()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var connection = TenantApiConnection.Create(
            tenantId: Guid.NewGuid(),
            tenantName: "Beta Store",
            platform: AdPlatform.TikTok,
            accountIdentifier: "tt_999",
            accountName: "TikTok Ads",
            tokenExpiresAtUtc: now.AddDays(-1),
            createdAtUtc: now.AddDays(-30)).Value;

        // Act
        connection.EvaluateExpiration(now, warningWindow: TimeSpan.FromDays(7));

        // Assert
        connection.Status.Should().Be(ApiConnectionStatus.Expired);
    }

    /// <summary>
    /// Verifies that MarkRevoked updates status and records error message.
    /// </summary>
    [Fact]
    public void MarkRevoked_ShouldSetRevokedStatusAndErrorMessage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var connection = TenantApiConnection.Create(
            tenantId: Guid.NewGuid(),
            tenantName: "Gama Store",
            platform: AdPlatform.Bing,
            accountIdentifier: "bing_111",
            accountName: "Microsoft Advertising",
            tokenExpiresAtUtc: now.AddDays(15),
            createdAtUtc: now).Value;

        // Act
        connection.MarkRevoked("User revoked permission in Microsoft Advertising dashboard.", now);

        // Assert
        connection.Status.Should().Be(ApiConnectionStatus.Revoked);
        connection.ErrorMessage.Should().Contain("User revoked permission");
        connection.UpdatedAtUtc.Should().Be(now);
    }

    /// <summary>
    /// Verifies that MarkConnected resets status to Connected and clears error message.
    /// </summary>
    [Fact]
    public void MarkConnected_ShouldResetStatusToConnectedAndClearErrors()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var connection = TenantApiConnection.Create(
            tenantId: Guid.NewGuid(),
            tenantName: "Delta Store",
            platform: AdPlatform.Google,
            accountIdentifier: "111-222-3333",
            accountName: "Google Ads",
            tokenExpiresAtUtc: now.AddDays(-2),
            createdAtUtc: now.AddDays(-60)).Value;

        connection.MarkRevoked("OAuth token expired", now.AddDays(-1));
        connection.Status.Should().Be(ApiConnectionStatus.Revoked);

        // Act
        var newExpiration = now.AddDays(90);
        connection.MarkConnected(newExpiration, now);

        // Assert
        connection.Status.Should().Be(ApiConnectionStatus.Connected);
        connection.ErrorMessage.Should().BeNull();
        connection.TokenExpiresAtUtc.Should().Be(newExpiration);
    }
}
