using Microsoft.Extensions.Caching.Memory;

namespace WebApp.Services.Social;

/// <summary>
/// Implementação de prévia de mídia com <see cref="IMemoryCache"/> e expiração curta.
/// </summary>
public sealed class SocialMediaPreviewCache : ISocialMediaPreviewCache
{
    private const string CacheKeyPrefix = "social-media-preview:";
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromMinutes(10);

    private readonly IMemoryCache _cache;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="SocialMediaPreviewCache"/>.
    /// </summary>
    /// <param name="cache">Cache em memória do host.</param>
    public SocialMediaPreviewCache(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public string Store(ReadOnlySpan<byte> content, string contentType)
    {
        var ticket = Guid.NewGuid().ToString("N");
        var payload = new CachedMediaPayload(content.ToArray(), contentType);
        _cache.Set(
            CacheKeyPrefix + ticket,
            payload,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = EntryLifetime });
        return ticket;
    }

    /// <inheritdoc />
    public bool TryGet(string ticket, out byte[]? content, out string? contentType)
    {
        content = null;
        contentType = null;

        if (string.IsNullOrWhiteSpace(ticket))
        {
            return false;
        }

        if (!_cache.TryGetValue(CacheKeyPrefix + ticket, out CachedMediaPayload? payload) || payload is null)
        {
            return false;
        }

        content = payload.Bytes;
        contentType = payload.ContentType;
        return content.Length > 0 && !string.IsNullOrWhiteSpace(contentType);
    }

    private sealed record CachedMediaPayload(byte[] Bytes, string ContentType);
}