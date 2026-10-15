using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.Meta;

/// <summary>Mídia enviada à conta Meta.</summary>
/// <param name="ExternalId">Hash da imagem ou ID do vídeo.</param>
/// <param name="MediaType">image ou video.</param>
/// <param name="State">Ready para imagens, Processing para vídeos ainda não consultados.</param>
public sealed record MetaMediaResult(string ExternalId, string MediaType, string State);

/// <summary>Upload de mídias sem publicação publicitária.</summary>
public interface IMetaMediaApi
{
    /// <summary>Valida e envia PNG/JPEG ou MP4 usando os endpoints oficiais.</summary>
    /// <param name="accountId">Conta Meta canônica.</param>
    /// <param name="token">Token backend.</param>
    /// <param name="contentType">Tipo MIME permitido.</param>
    /// <param name="fileName">Nome do arquivo.</param>
    /// <param name="content">Conteúdo limitado.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Identificador da mídia ou falha sanitizada.</returns>
    Task<Result<MetaMediaResult>> UploadAsync(string accountId, string token, string contentType, string fileName, byte[] content, CancellationToken cancellationToken);

    /// <summary>Consulta o processamento de um vídeo cujo vínculo local já foi confirmado.</summary>
    /// <param name="videoId">ID externo de vídeo enviado por esta conta.</param>
    /// <param name="token">Token backend.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Estado oficial do processamento.</returns>
    Task<Result<MetaMediaResult>> GetVideoStatusAsync(string videoId, string token, CancellationToken cancellationToken);
}
