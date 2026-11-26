namespace SocialPublishing.Application.Media;

/// <summary>
/// Formata a chave estável da logo do cliente no bucket orgânico.
/// </summary>
public static class BrandLogoObjectKeyFormatter
{
    /// <summary>
    /// Monta a chave relativa no bucket orgânico.
    /// </summary>
    /// <param name="keyPrefix">Prefixo configurado (padrão <c>organic-media</c>).</param>
    /// <param name="tenantId">Tenant dono do objeto.</param>
    /// <param name="workspaceId">Workspace do cliente.</param>
    /// <returns>Chave no formato <c>{prefix}/{tenant:N}/brand-voice/{workspace:N}/logo</c>.</returns>
    public static string Build(string keyPrefix, Guid tenantId, Guid workspaceId)
    {
        var prefix = string.IsNullOrWhiteSpace(keyPrefix) ? "organic-media" : keyPrefix.Trim();
        return $"{prefix}/{tenantId:N}/brand-voice/{workspaceId:N}/logo";
    }

    /// <summary>
    /// Extrai o tenant embutido na chave canônica da logo.
    /// </summary>
    /// <param name="objectKey">Chave persistida.</param>
    /// <param name="keyPrefix">Prefixo esperado.</param>
    /// <param name="tenantId">Tenant parseado.</param>
    /// <returns><see langword="true"/> quando a chave segue o layout esperado.</returns>
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

        if (!string.Equals(segments[2], "brand-voice", StringComparison.Ordinal)
            || !string.Equals(segments[4], "logo", StringComparison.Ordinal))
        {
            return false;
        }

        return Guid.TryParseExact(segments[1], "N", out tenantId);
    }
}