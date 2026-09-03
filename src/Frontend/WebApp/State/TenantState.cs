namespace WebApp.State;

/// <summary>
/// Representa o estado contextual do Tenant ativo na sessão Blazor.
/// </summary>
/// <param name="TenantId">Identificador único global do Tenant.</param>
/// <param name="Name">Nome de exibição da organização.</param>
/// <param name="Slug">Slug identificador para rotas e subdomínio.</param>
/// <param name="CustomDomain">Domínio personalizado CNAME mapeado (ex.: analytics.suaempresa.com.br).</param>
/// <param name="Branding">Configurações de identidade visual e White-Label.</param>
public record TenantState(
    Guid TenantId,
    string Name,
    string Slug,
    string? CustomDomain,
    TenantBranding Branding)
{
    /// <summary>
    /// Instância padrão do sistema para inicialização antes da identificação de tenant específico.
    /// </summary>
    public static TenantState Default => new(
        TenantId: Guid.Empty,
        Name: "AdMetricsPro",
        Slug: "default",
        CustomDomain: null,
        Branding: TenantBranding.Default);
}
