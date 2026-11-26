namespace SocialPublishing.Application.Media;

/// <summary>
/// Metadado mínimo de um ativo orgânico para auditoria de migração de volume (Fase 5).
/// </summary>
/// <param name="ObjectKey">Chave no layout orgânico.</param>
/// <param name="MimeType">Tipo MIME persistido no SQL, se houver.</param>
public sealed record OrganicMediaCatalogEntry(string ObjectKey, string? MimeType);