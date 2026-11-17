using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Tenants.Application.Users.Services;

namespace Tenants.Infrastructure.Users;

/// <summary>
/// Implementação criptográfica de <see cref="ITenantInvitationTokenService"/>.
/// </summary>
public sealed class TenantInvitationTokenService : ITenantInvitationTokenService
{
    /// <inheritdoc />
    public (string RawToken, string TokenHash) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = WebEncoders.Base64UrlEncode(bytes);
        return (rawToken, Hash(rawToken));
    }

    /// <inheritdoc />
    public string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}