namespace WebApp.Components.Social;

/// <summary>
/// Normaliza e decompõe legendas de posts orgânicos para exibição na UI.
/// </summary>
public static class OrganicPostCaptionDisplay
{
    /// <summary>
    /// Partes da legenda preparadas para renderização.
    /// </summary>
    /// <param name="Body">Texto principal com quebras de linha reais.</param>
    /// <param name="Hashtags">Hashtags extraídas da(s) linha(s) final(is) somente com tags.</param>
    public readonly record struct Parts(string Body, IReadOnlyList<string> Hashtags);

    /// <summary>
    /// Converte sequências literais <c>\n</c>, <c>\r</c> e <c>\r\n</c> em quebras de linha reais.
    /// </summary>
    /// <param name="caption">Texto persistido na API.</param>
    /// <returns>Legenda normalizada.</returns>
    public static string NormalizeLiteralEscapes(string caption)
    {
        if (string.IsNullOrEmpty(caption))
        {
            return string.Empty;
        }

        return caption
            .Replace("\\r\\n", "\n", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\r", "\n", StringComparison.Ordinal)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    /// <summary>
    /// Separa o corpo da legenda das hashtags em linha dedicada ao final do texto.
    /// </summary>
    /// <param name="caption">Legenda bruta ou já normalizada.</param>
    /// <returns>Corpo e lista de hashtags sem duplicatas de ordem.</returns>
    public static Parts Parse(string? caption)
    {
        if (string.IsNullOrWhiteSpace(caption))
        {
            return new Parts(string.Empty, Array.Empty<string>());
        }

        var normalized = NormalizeLiteralEscapes(caption);
        var lines = normalized.Split('\n');
        var hashtags = new List<string>();
        var bodyEnd = lines.Length;

        while (bodyEnd > 0)
        {
            var index = bodyEnd - 1;
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                if (hashtags.Count > 0)
                {
                    bodyEnd = index;
                    continue;
                }

                bodyEnd = index;
                continue;
            }

            if (!IsHashtagOnlyLine(line))
            {
                break;
            }

            hashtags.InsertRange(0, ExtractHashtags(line));
            bodyEnd = index;
        }

        var body = string.Join('\n', lines.Take(bodyEnd)).TrimEnd();
        var distinctHashtags = new List<string>(hashtags.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in hashtags)
        {
            if (seen.Add(tag))
            {
                distinctHashtags.Add(tag);
            }
        }

        return new Parts(body, distinctHashtags);
    }

    /// <summary>
    /// Colapsa quebras para prévia compacta em cards do calendário.
    /// </summary>
    /// <param name="caption">Legenda integral.</param>
    /// <returns>Texto em linha única.</returns>
    public static string CollapseForCardPreview(string caption)
    {
        var normalized = NormalizeLiteralEscapes(caption);
        return normalized.Replace('\n', ' ').Replace("  ", " ", StringComparison.Ordinal).Trim();
    }

    private static bool IsHashtagOnlyLine(string line)
    {
        var tokens = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length > 0 && tokens.All(t => t.StartsWith('#'));
    }

    private static IEnumerable<string> ExtractHashtags(string line) =>
        line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
}