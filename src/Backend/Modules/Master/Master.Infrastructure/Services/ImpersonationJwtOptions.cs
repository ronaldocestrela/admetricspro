namespace Master.Infrastructure.Services;

/// <summary>
/// Configuration options for signing and validating tenant impersonation JWT tokens.
/// </summary>
public sealed class ImpersonationJwtOptions
{
    /// <summary>
    /// Configuration section key name in application configuration files.
    /// </summary>
    public const string SectionName = "ImpersonationJwt";

    /// <summary>
    /// Gets or sets the token issuer name.
    /// </summary>
    public string Issuer { get; set; } = "AdMetricsPro.Master";

    /// <summary>
    /// Gets or sets the target audience for the token.
    /// </summary>
    public string Audience { get; set; } = "AdMetricsPro.Tenants";

    /// <summary>
    /// Gets or sets the symmetric secret signing key (minimum 32 characters / 256 bits).
    /// </summary>
    public string SecretKey { get; set; } = "AdMetricsPro_Secret_Key_For_Tenant_Impersonation_Security_2026_Minimum_32_Chars!";

    /// <summary>
    /// Gets or sets the default token expiration in minutes.
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 30;
}
