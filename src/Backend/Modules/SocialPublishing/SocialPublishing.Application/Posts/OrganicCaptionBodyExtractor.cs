namespace SocialPublishing.Application.Posts;

/// <summary>
/// Extrai o corpo da legenda sem a linha final de hashtags.
/// </summary>
public static class OrganicCaptionBodyExtractor
{
    /// <summary>
    /// Remove hashtags em linhas dedicadas ao final do texto persistido.
    /// </summary>
    /// <param name="caption">Legenda completa do post.</param>
    /// <returns>Corpo textual para briefs criativos.</returns>
    public static string GetBodyWithoutHashtags(string? caption)
    {
        if (string.IsNullOrWhiteSpace(caption))
        {
            return string.Empty;
        }

        var normalized = caption
            .Replace("\\r\\n", "\n", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\r", "\n", StringComparison.Ordinal)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        var lines = normalized.Split('\n');
        var bodyEnd = lines.Length;

        while (bodyEnd > 0)
        {
            var index = bodyEnd - 1;
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                bodyEnd = index;
                continue;
            }

            if (IsHashtagOnlyLine(line))
            {
                bodyEnd = index;
                continue;
            }

            break;
        }

        return string.Join('\n', lines.Take(bodyEnd)).Trim();
    }

    private static bool IsHashtagOnlyLine(string line)
    {
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length > 0 && tokens.All(t => t.StartsWith("#", StringComparison.Ordinal));
    }
}