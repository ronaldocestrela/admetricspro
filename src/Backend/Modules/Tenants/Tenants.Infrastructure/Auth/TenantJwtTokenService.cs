using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tenants.Application.Auth.Services;

namespace Tenants.Infrastructure.Auth;

/// <summary>
/// Implementação de <see cref="ITenantTokenService"/> para geração de tokens JWT assinados digitalmente
/// com algoritmo HMAC-SHA256 para autenticação segura de operadores e gestores de inquilinos.
/// </summary>
public sealed class TenantJwtTokenService : ITenantTokenService
{
    private readonly TenantJwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantJwtTokenService"/>.
    /// </summary>
    /// <param name="options">Opções de configuração de JWT para inquilinos.</param>
    public TenantJwtTokenService(IOptions<TenantJwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SecretKey) || _options.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Tenant JWT secret key must have at least 32 characters (256 bits).");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
    }

    /// <inheritdoc />
    public Result<string> GenerateToken(TenantUser user, Guid tenantId, string subdomain)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (tenantId == Guid.Empty)
        {
            return Result<string>.Failure(Error.Validation("Tenant.InvalidId", "TenantId cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return Result<string>.Failure(Error.Validation("Tenant.InvalidSubdomain", "Subdomain cannot be empty."));
        }

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.FullName),
            new("tenant_id", tenantId.ToString()),
            new("tenant_subdomain", subdomain),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var tokenString = handler.WriteToken(tokenDescriptor);

        return Result<string>.Success(tokenString);
    }
}
