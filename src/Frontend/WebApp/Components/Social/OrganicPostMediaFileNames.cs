namespace WebApp.Components.Social;

/// <summary>
/// Gera nomes de arquivo sugeridos para mídias de posts orgânicos no download no navegador.
/// </summary>
public static class OrganicPostMediaFileNames
{
    /// <summary>
    /// Monta o nome do arquivo de imagem do post a partir do identificador e do tipo MIME.
    /// </summary>
    /// <param name="postId">Identificador do post orgânico.</param>
    /// <param name="mimeType">Tipo MIME da mídia (ex.: image/png).</param>
    /// <returns>Nome de arquivo no formato post-{id}.{ext}.</returns>
    public static string BuildDownloadFileName(Guid postId, string mimeType)
    {
        var extension = ResolveExtension(mimeType);
        return $"post-{postId}.{extension}";
    }

    private static string ResolveExtension(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return "bin";
        }

        var normalized = mimeType.Trim().ToLowerInvariant();
        return normalized switch
        {
            "image/png" => "png",
            "image/jpeg" or "image/jpg" => "jpg",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ when normalized.StartsWith("image/", StringComparison.Ordinal) => "img",
            _ => "bin",
        };
    }
}