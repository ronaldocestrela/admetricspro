namespace WebApi.Configuration;

/// <summary>
/// Opções de armazenamento de objetos (MinIO / S3). Na Fase 1 apenas o health check consome <see cref="Provider"/> e <see cref="ServiceUrl"/>.
/// </summary>
public sealed class ObjectStorageOptions
{
    /// <summary>
    /// Nome da seção em <c>IConfiguration</c> (variáveis de ambiente <c>ObjectStorage__*</c>).
    /// </summary>
    public const string SectionName = "ObjectStorage";

    /// <summary>
    /// Provedor ativo: <c>Local</c> (padrão, disco da API) ou <c>MinIO</c>.
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Endpoint HTTP(S) interno da API S3 do MinIO (ex.: <c>http://minio:9000</c>).
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Access key do cluster (usado nas fases seguintes; não lida pelo health check da Fase 1).
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret key do cluster (usado nas fases seguintes; não lida pelo health check da Fase 1).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Região exigida pelo SDK S3 (ex.: <c>us-east-1</c>).
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Nome do bucket de mídia orgânica.
    /// </summary>
    public string OrganicBucket { get; set; } = "admetricspro-organic";

    /// <summary>
    /// Nome do bucket de PDFs de relatório.
    /// </summary>
    public string ReportsBucket { get; set; } = "admetricspro-reports";

    /// <summary>
    /// Nome do bucket de logos e favicon enviados pela agência.
    /// </summary>
    public string BrandingBucket { get; set; } = "admetricspro-branding";

    /// <summary>
    /// Indica se o health check do MinIO deve ser registrado.
    /// </summary>
    /// <param name="options">Opções materializadas da seção <c>ObjectStorage</c>.</param>
    /// <returns><see langword="true"/> quando <see cref="Provider"/> é <c>MinIO</c> (qualquer capitalização).</returns>
    public static bool ShouldRegisterMinioHealthCheck(ObjectStorageOptions options) =>
        string.Equals(options.Provider, "MinIO", StringComparison.OrdinalIgnoreCase);
}