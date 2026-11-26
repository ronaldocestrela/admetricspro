namespace Tenants.Application.Branding;

/// <summary>
/// Monta URLs públicas da API para ativos de branding hospedados no object storage.
/// </summary>
public interface ITenantBrandPublicUrlResolver
{
    /// <summary>
    /// Resolve a URL absoluta do endpoint público de leitura.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="slot">Slot do ativo.</param>
    /// <param name="requestOrigin">Origem HTTPS da requisição atual (fallback quando <c>PublicBaseUrl</c> está vazio).</param>
    /// <returns>URL absoluta ou <see langword="null"/> se não houver origem utilizável.</returns>
    string? Resolve(Guid tenantId, TenantBrandAssetSlot slot, string? requestOrigin);

    /// <summary>
    /// Indica se a URL informada é a URL pública da API gerada para o slot.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="slot">Slot do ativo.</param>
    /// <param name="url">URL a comparar.</param>
    /// <param name="requestOrigin">Origem HTTPS da requisição atual.</param>
    /// <returns><see langword="true"/> quando equivalente à URL resolvida.</returns>
    bool IsResolvedApiUrl(Guid tenantId, TenantBrandAssetSlot slot, string? url, string? requestOrigin);
}