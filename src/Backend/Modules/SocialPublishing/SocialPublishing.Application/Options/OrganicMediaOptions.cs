namespace SocialPublishing.Application.Options;

/// <summary>
/// Opções de armazenamento de mídia orgânica.
/// </summary>
public sealed class OrganicMediaOptions
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string SectionName = "SocialPublishing:Media";

    /// <summary>
    /// Provedor de armazenamento: Local ou S3.
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Diretório raiz para armazenamento local.
    /// </summary>
    public string LocalRoot { get; set; } = "./data/social-media";

    /// <summary>
    /// Nome do bucket S3-compatível.
    /// </summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// URL do serviço S3-compatível (opcional).
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Prefixo de chave antes de tenant/workspace/asset.
    /// </summary>
    public string KeyPrefix { get; set; } = "organic-media";

    /// <summary>
    /// Tamanho máximo de upload em bytes.
    /// </summary>
    public long MaxUploadBytes { get; set; } = 110_000_000;

    /// <summary>
    /// URL base HTTPS pública para objetos de mídia (ex.: CDN ou proxy de leitura).
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
