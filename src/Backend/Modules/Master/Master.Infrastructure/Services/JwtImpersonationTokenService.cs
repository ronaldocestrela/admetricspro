using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Services;
using Master.Domain.Tenants;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Master.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IImpersonationTokenService"/> using signed JSON Web Tokens (JWT)
/// to issue and validate scoped, audited tenant impersonation sessions.
/// </summary>
public sealed class JwtImpersonationTokenService : IImpersonationTokenService
{
    private readonly ImpersonationJwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtImpersonationTokenService"/> class.
    /// </summary>
    /// <param name="options">Impersonation JWT configuration options.</param>
    public JwtImpersonationTokenService(IOptions<ImpersonationJwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SecretKey) || _options.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Impersonation JWT secret key must have at least 32 characters (256 bits).");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
    }

    /// <inheritdoc />
    public Result<string> GenerateToken(ImpersonationSession session, Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(tenant);

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, session.SuperAdminId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ImpersonationClaims.IsImpersonated, "true"),
            new(ImpersonationClaims.OriginalSuperAdminId, session.SuperAdminId.ToString()),
            new(ImpersonationClaims.TenantId, tenant.Id.Value.ToString()),
            new(ImpersonationClaims.SupportTicketId, session.SupportTicketId),
            new(ImpersonationClaims.SessionId, session.Id.ToString()),
            new(ClaimTypes.Name, tenant.CompanyName)
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: session.CreatedAtUtc,
            expires: session.ExpiresAtUtc,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var tokenString = handler.WriteToken(tokenDescriptor);

        return Result<string>.Success(tokenString);
    }

    /// <inheritdoc />
    public Result<ClaimsPrincipal> ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<ClaimsPrincipal>.Failure(
                Error.Validation("ImpersonationToken.Empty", "Token cannot be null or empty."));
        }

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return Result<ClaimsPrincipal>.Failure(
                    Error.Failure("ImpersonationToken.InvalidAlgorithm", "Token signing algorithm is invalid."));
            }

            var isImpersonatedClaim = principal.FindFirst(ImpersonationClaims.IsImpersonated);
            if (isImpersonatedClaim is null || !string.Equals(isImpersonatedClaim.Value, "true", StringComparison.OrdinalIgnoreCase))
            {
                return Result<ClaimsPrincipal>.Failure(
                    Error.Failure("ImpersonationToken.NotImpersonated", "Token does not represent an active impersonation session."));
            }

            return Result<ClaimsPrincipal>.Success(principal);
        }
        catch (SecurityTokenExpiredException)
        {
            return Result<ClaimsPrincipal>.Failure(
                Error.Failure("ImpersonationToken.Expired", "The impersonation token has expired."));
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return Result<ClaimsPrincipal>.Failure(
                Error.Failure("ImpersonationToken.Invalid", "The impersonation token signature or structure is invalid."));
        }
    }
}
