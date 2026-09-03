using BuildingBlocks.Application.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.MultiTenancy.Strategies;

/// <summary>
/// Identifies tenant identity from claims in the authenticated user's JWT token.
/// </summary>
public sealed class JwtClaimTenantIdentificationStrategy : ITenantIdentificationStrategy
{
    private const string StandardMicrosoftTenantClaim = "http://schemas.microsoft.com/identity/claims/tenantid";
    private readonly TenantResolutionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtClaimTenantIdentificationStrategy"/> class.
    /// </summary>
    /// <param name="options">Tenant resolution configuration options.</param>
    public JwtClaimTenantIdentificationStrategy(IOptions<TenantResolutionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public TenantResolutionSource Source => TenantResolutionSource.JwtClaim;

    /// <inheritdoc />
    public ValueTask<TenantIdentificationResult?> IdentifyTenantAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var user = httpContext.User;
        if (user.Identity is not { IsAuthenticated: true })
        {
            return ValueTask.FromResult<TenantIdentificationResult?>(null);
        }

        var claim = user.FindFirst(_options.JwtClaimType) ?? user.FindFirst(StandardMicrosoftTenantClaim);
        if (claim is null || string.IsNullOrWhiteSpace(claim.Value))
        {
            return ValueTask.FromResult<TenantIdentificationResult?>(null);
        }

        var rawValue = claim.Value.Trim();

        if (Guid.TryParse(rawValue, out var tenantId))
        {
            return ValueTask.FromResult<TenantIdentificationResult?>(
                TenantIdentificationResult.FromTenantId(tenantId, Source, rawValue));
        }

        return ValueTask.FromResult<TenantIdentificationResult?>(
            TenantIdentificationResult.FromSubdomain(rawValue.ToLowerInvariant(), Source, rawValue));
    }
}
