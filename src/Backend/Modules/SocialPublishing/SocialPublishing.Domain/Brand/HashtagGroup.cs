using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Domain.Brand;

/// <summary>
/// Value object que representa um conjunto de hashtags validado contra política e teto da rede.
/// </summary>
public sealed class HashtagGroup : ValueObject
{
    private readonly List<string> _tags;

    private HashtagGroup(IReadOnlyList<string> tags)
    {
        _tags = tags.ToList();
    }

    /// <summary>
    /// Obtém as hashtags normalizadas (sem duplicatas).
    /// </summary>
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Cria um grupo de hashtags respeitando teto efetivo e regras de formato.
    /// </summary>
    /// <param name="platform">Plataforma de destino.</param>
    /// <param name="profileHashtagLimit">Teto configurado no perfil de marca.</param>
    /// <param name="hashtags">Hashtags informadas.</param>
    /// <returns>Resultado com o grupo ou erro de política.</returns>
    public static Result<HashtagGroup> Create(
        OrganicPlatform platform,
        int profileHashtagLimit,
        IEnumerable<string> hashtags)
    {
        var networkLimit = GetNetworkHashtagLimit(platform);
        var effectiveLimit = Math.Min(profileHashtagLimit, networkLimit);

        if (profileHashtagLimit <= 0)
        {
            return Result<HashtagGroup>.Failure(
                Error.Validation("Social.HashtagLimitInvalid", "O teto de hashtags do perfil deve ser positivo."));
        }

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in hashtags)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var tag = raw.Trim();
            if (tag.StartsWith('#'))
            {
                tag = tag[1..];
            }

            if (tag.Contains(' ', StringComparison.Ordinal))
            {
                return Result<HashtagGroup>.Failure(SocialErrors.HashtagPolicyViolated);
            }

            if (!seen.Add(tag))
            {
                return Result<HashtagGroup>.Failure(SocialErrors.HashtagPolicyViolated);
            }

            normalized.Add(tag);
        }

        if (normalized.Count > effectiveLimit)
        {
            return Result<HashtagGroup>.Failure(SocialErrors.HashtagPolicyViolated);
        }

        return Result<HashtagGroup>.Success(new HashtagGroup(normalized));
    }

    /// <summary>
    /// Obtém o teto hard de hashtags da rede.
    /// </summary>
    /// <param name="platform">Plataforma orgânica.</param>
    /// <returns>Quantidade máxima de hashtags.</returns>
    public static int GetNetworkHashtagLimit(OrganicPlatform platform) => platform switch
    {
        OrganicPlatform.Instagram => 30,
        OrganicPlatform.FacebookPage => 30,
        OrganicPlatform.TikTok => 8,
        _ => 30,
    };

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var tag in _tags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
        {
            yield return tag.ToLowerInvariant();
        }
    }
}
