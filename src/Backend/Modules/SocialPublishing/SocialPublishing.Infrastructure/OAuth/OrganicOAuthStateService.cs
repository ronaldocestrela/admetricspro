using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Infrastructure.OAuth;

/// <summary>
/// Implementação de <see cref="IOrganicOAuthStateService"/> com payload JSON Base64Url e HMAC-SHA256 (TTL 15 minutos).
/// </summary>
public sealed class OrganicOAuthStateService : IOrganicOAuthStateService
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan FutureSkew = TimeSpan.FromSeconds(30);
    private readonly byte[] _signingKey;

    /// <summary>
    /// Inicializa o serviço com a chave de assinatura HMAC.
    /// </summary>
    /// <param name="signingKey">Chave secreta (mínimo recomendado 32 caracteres).</param>
    public OrganicOAuthStateService(string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new ArgumentException("A chave de assinatura do state OAuth orgânico não pode ser vazia.", nameof(signingKey));
        }

        _signingKey = Encoding.UTF8.GetBytes(signingKey);
    }

    /// <inheritdoc />
    public OrganicOAuthStateIssueResult Issue(
        Guid tenantId,
        Guid workspaceId,
        OrganicPlatform platform,
        string redirectUri,
        DateTime issuedAtUtc)
    {
        var payload = new OrganicOAuthStatePayload(
            tenantId,
            workspaceId,
            platform,
            redirectUri,
            issuedAtUtc,
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

        var payloadBase64Url = EncodePayload(payload);
        var signatureBase64Url = Sign(payloadBase64Url);
        var state = $"{payloadBase64Url}.{signatureBase64Url}";

        return new OrganicOAuthStateIssueResult(state, issuedAtUtc.Add(StateLifetime));
    }

    /// <inheritdoc />
    public Result<OrganicOAuthStatePayload> Validate(
        string state,
        Guid expectedTenantId,
        Guid expectedWorkspaceId,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return InvalidState();
        }

        var parts = state.Split('.');
        if (parts.Length != 2)
        {
            return InvalidState();
        }

        var payloadBase64Url = parts[0];
        var signatureBase64Url = parts[1];
        var expectedSignature = Sign(payloadBase64Url);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signatureBase64Url),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return InvalidState();
        }

        OrganicOAuthStatePayload? payload;
        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(payloadBase64Url));
            payload = JsonSerializer.Deserialize<OrganicOAuthStatePayload>(json);
        }
        catch
        {
            return InvalidState();
        }

        if (payload is null)
        {
            return InvalidState();
        }

        if (utcNow - payload.IssuedAtUtc > StateLifetime ||
            payload.IssuedAtUtc > utcNow.Add(FutureSkew))
        {
            return InvalidState();
        }

        if (payload.TenantId != expectedTenantId || payload.WorkspaceId != expectedWorkspaceId)
        {
            return InvalidState();
        }

        return Result<OrganicOAuthStatePayload>.Success(payload);
    }

    private string Sign(string payloadBase64Url)
    {
        using var hmac = new HMACSHA256(_signingKey);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64Url));
        return Base64UrlEncode(signatureBytes);
    }

    private static string EncodePayload(OrganicOAuthStatePayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    private static Result<OrganicOAuthStatePayload> InvalidState() =>
        Result<OrganicOAuthStatePayload>.Failure(SocialErrors.OAuthStateInvalid);

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
