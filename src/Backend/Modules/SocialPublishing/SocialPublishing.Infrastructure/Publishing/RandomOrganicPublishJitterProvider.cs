using SocialPublishing.Application.Abstractions;

namespace SocialPublishing.Infrastructure.Publishing;

/// <summary>
/// Jitter pseudoaleatório para backoff de publicação orgânica.
/// </summary>
public sealed class RandomOrganicPublishJitterProvider : IOrganicPublishJitterProvider
{
    /// <inheritdoc />
    public int GetJitterMilliseconds() => Random.Shared.Next(0, 250);
}
