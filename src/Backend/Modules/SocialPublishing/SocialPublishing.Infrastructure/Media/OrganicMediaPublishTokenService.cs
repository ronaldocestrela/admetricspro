using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Tokens HMAC-SHA256 de leitura pública de mídia orgânica.
/// </summary>
public sealed class OrganicMediaPublishTokenService : IOrganicMediaPublishTokenService
{
    private const int MaxTtlMinutes = 1440;
    private static readonly TimeSpan FutureSkew = TimeSpan.FromSeconds(30);
    private readonly byte[] _signingKey;
    private readonly ObjectStorageOptions _options;
    private readonly OrganicMediaOptions _mediaOptions;

    /// <summary>
    /// Inicializa o serviço de tokens de publicação.
    /// </summary>
    /// <param name="options">Opções de object storage (TTL e chave HMAC).</param>
    /// <param name="mediaOptions">Opções de mídia (prefixo de chave).</param>
    public OrganicMediaPublishTokenService(
        IOptions<ObjectStorageOptions> options,
        IOptions<OrganicMediaOptions> mediaOptions)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _mediaOptions = mediaOptions?.Value ?? throw new ArgumentNullException(nameof(mediaOptions));

        _signingKey = string.IsNullOrWhiteSpace(_options.PublishUrlSigningKey)
            ? []
            : Encoding.UTF8.GetBytes(_options.PublishUrlSigningKey);
    }

    /// <inheritdoc />
    public Result<string> IssueToken(
        Guid tenantId,
        string objectKey,
        string contentType,
        DateTime utcNow)
    {
        if (_signingKey.Length < 32
            || tenantId == Guid.Empty
            || string.IsNullOrWhiteSpace(objectKey)
            || string.IsNullOrWhiteSpace(contentType))
        {
            return Result<string>.Failure(StorageErrors.KeyInvalid);
        }

        if (!OrganicMediaObjectKeyFormatter.TryGetTenantId(objectKey, _mediaOptions.KeyPrefix, out var keyTenant)
            || keyTenant != tenantId)
        {
            return Result<string>.Failure(StorageErrors.KeyInvalid);
        }

        var ttlMinutes = _options.PublishUrlTtlMinutes <= 0 ? 120 : _options.PublishUrlTtlMinutes;
        if (ttlMinutes > MaxTtlMinutes)
        {
            ttlMinutes = MaxTtlMinutes;
        }

        var expiresAt = utcNow.AddMinutes(ttlMinutes);
        var payload = new OrganicMediaPublishTokenPayloadRecord(
            tenantId,
            objectKey.Trim(),
            contentType.Trim(),
            expiresAt);

        var payloadBase64Url = EncodePayload(payload);
        var signatureBase64Url = Sign(payloadBase64Url);
        return Result<string>.Success($"{payloadBase64Url}.{signatureBase64Url}");
    }

    /// <inheritdoc />
    public Result<OrganicMediaPublishTokenPayload> ValidateToken(string token, DateTime utcNow)
    {
        if (_signingKey.Length < 32 || string.IsNullOrWhiteSpace(token))
        {
            return InvalidToken();
        }

        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            return InvalidToken();
        }

        var payloadBase64Url = parts[0];
        var signatureBase64Url = parts[1];
        var expectedSignature = Sign(payloadBase64Url);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signatureBase64Url),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return InvalidToken();
        }

        OrganicMediaPublishTokenPayloadRecord? payload;
        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(payloadBase64Url));
            payload = JsonSerializer.Deserialize<OrganicMediaPublishTokenPayloadRecord>(json);
        }
        catch
        {
            return InvalidToken();
        }

        if (payload is null
            || payload.TenantId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.ObjectKey)
            || string.IsNullOrWhiteSpace(payload.ContentType))
        {
            return InvalidToken();
        }

        if (utcNow > payload.ExpiresAtUtc || payload.ExpiresAtUtc < utcNow.Subtract(TimeSpan.FromDays(2)))
        {
            return InvalidToken();
        }

        if (payload.ExpiresAtUtc > utcNow.Add(FutureSkew).AddMinutes(MaxTtlMinutes))
        {
            return InvalidToken();
        }

        if (!OrganicMediaObjectKeyFormatter.TryGetTenantId(
                payload.ObjectKey,
                _mediaOptions.KeyPrefix,
                out var keyTenant)
            || keyTenant != payload.TenantId)
        {
            return InvalidToken();
        }

        return Result<OrganicMediaPublishTokenPayload>.Success(
            new OrganicMediaPublishTokenPayload(
                payload.TenantId,
                payload.ObjectKey,
                payload.ContentType,
                payload.ExpiresAtUtc));
    }

    private string Sign(string payloadBase64Url)
    {
        using var hmac = new HMACSHA256(_signingKey);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64Url));
        return Base64UrlEncode(signatureBytes);
    }

    private static string EncodePayload(OrganicMediaPublishTokenPayloadRecord payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    private static Result<OrganicMediaPublishTokenPayload> InvalidToken() =>
        Result<OrganicMediaPublishTokenPayload>.Failure(SocialErrors.PublicMediaNotFound);

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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

    private sealed record OrganicMediaPublishTokenPayloadRecord(
        Guid TenantId,
        string ObjectKey,
        string ContentType,
        DateTime ExpiresAtUtc);
}