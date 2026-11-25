namespace WebApp.Services.Social;

/// <summary>
/// Cache efêmero de bytes de mídia para prévia no Blazor Server sem data URLs no circuito SignalR.
/// </summary>
public interface ISocialMediaPreviewCache
{
    /// <summary>
    /// Armazena o conteúdo e devolve um ticket opaco para URL curta.
    /// </summary>
    /// <param name="content">Bytes da imagem.</param>
    /// <param name="contentType">Tipo MIME (ex.: image/png).</param>
    /// <returns>Ticket utilizável em <c>/social/media-preview/{ticket}</c>.</returns>
    string Store(ReadOnlySpan<byte> content, string contentType);

    /// <summary>
    /// Recupera o conteúdo previamente armazenado.
    /// </summary>
    /// <param name="ticket">Ticket retornado por <see cref="Store"/>.</param>
    /// <param name="content">Bytes recuperados.</param>
    /// <param name="contentType">Tipo MIME armazenado.</param>
    /// <returns><c>true</c> quando o ticket é válido e não expirou.</returns>
    bool TryGet(string ticket, out byte[]? content, out string? contentType);
}