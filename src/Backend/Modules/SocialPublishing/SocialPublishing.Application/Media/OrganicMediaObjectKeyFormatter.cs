using SocialPublishing.Application.Abstractions;

namespace SocialPublishing.Application.Media;

/// <summary>
/// Formata chaves de objeto de mídia orgânica (staging e durable).
/// </summary>
public static class OrganicMediaObjectKeyFormatter
{
    /// <summary>
    /// Monta a chave relativa no bucket orgânico.
    /// </summary>
    /// <param name="keyPrefix">Prefixo configurado (padrão <c>organic-media</c>).</param>
    /// <param name="tenantId">Tenant dono do objeto.</param>
    /// <param name="workspaceId">Workspace do ativo.</param>
    /// <param name="assetId">Identificador do ativo de mídia.</param>
    /// <param name="tier">Estágio de gravação (staging ou durable).</param>
    /// <returns>Chave no formato <c>{prefix}/staging|durable/{tenant:N}/{workspace:N}/{asset:N}</c>.</returns>
    public static string Build(
        string keyPrefix,
        Guid tenantId,
        Guid workspaceId,
        Guid assetId,
        OrganicMediaStorageTier tier)
    {
        var prefix = string.IsNullOrWhiteSpace(keyPrefix) ? "organic-media" : keyPrefix.Trim();
        var segment = tier == OrganicMediaStorageTier.Staging ? "staging" : "durable";
        return $"{prefix}/{segment}/{tenantId:N}/{workspaceId:N}/{assetId:N}";
    }

    /// <summary>
    /// Extrai o tenant embutido na chave orgânica canônica.
    /// </summary>
    /// <param name="objectKey">Chave persistida.</param>
    /// <param name="keyPrefix">Prefixo esperado.</param>
    /// <param name="tenantId">Tenant parseado.</param>
    /// <returns><see langword="true"/> quando a chave segue o layout orgânico.</returns>
    public static bool TryGetTenantId(string objectKey, string keyPrefix, out Guid tenantId)
    {
        tenantId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return false;
        }

        var prefix = string.IsNullOrWhiteSpace(keyPrefix) ? "organic-media" : keyPrefix.Trim();
        var segments = objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 5)
        {
            return false;
        }

        if (!string.Equals(segments[0], prefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(segments[1], "staging", StringComparison.Ordinal)
            && !string.Equals(segments[1], "durable", StringComparison.Ordinal))
        {
            return false;
        }

        return Guid.TryParseExact(segments[2], "N", out tenantId);
    }
}