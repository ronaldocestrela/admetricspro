using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Planning.Copy;

/// <summary>
/// Compõe a legenda persistida no post a partir da cópia aceita e dos grupos de hashtags.
/// </summary>
public static class EditorialCaptionComposer
{
    /// <summary>
    /// Concatena legenda e hashtags validadas, respeitando o teto da rede.
    /// </summary>
    /// <param name="platform">Rede de destino.</param>
    /// <param name="caption">Legenda validada pelo gate de aceitação.</param>
    /// <param name="hashtagGroups">Grupos de hashtags normalizados.</param>
    /// <returns>Texto final ou erro se exceder <see cref="CaptionLimits"/>.</returns>
    public static Result<string> Compose(OrganicPlatform platform, string caption, GeneratedHashtagGroupsDto hashtagGroups)
    {
        var trimmedCaption = caption.Trim();
        var tags = hashtagGroups.Brand
            .Concat(hashtagGroups.Niche)
            .Concat(hashtagGroups.Reach)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(NormalizeTag)
            .ToList();

        if (tags.Count == 0)
        {
            return ValidateLength(platform, trimmedCaption);
        }

        var hashtagLine = string.Join(' ', tags);
        var composed = $"{trimmedCaption}{Environment.NewLine}{Environment.NewLine}{hashtagLine}";
        return ValidateLength(platform, composed);
    }

    private static string NormalizeTag(string tag)
    {
        var trimmed = tag.Trim();
        return trimmed.StartsWith('#') ? trimmed : $"#{trimmed}";
    }

    private static Result<string> ValidateLength(OrganicPlatform platform, string text)
    {
        var max = CaptionLimits.GetMaxLength(platform);
        if (text.Length > max)
        {
            return Result<string>.Failure(SocialErrors.CaptionTooLong);
        }

        return Result<string>.Success(text);
    }
}
