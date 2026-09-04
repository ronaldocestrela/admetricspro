namespace BackofficeApp.State;

/// <summary>
/// Representa as configurações de identidade visual e personalização White-Label do Tenant.
/// </summary>
/// <param name="PrimaryColor">Código hexadecimal da cor primária (ex.: #2563EB).</param>
/// <param name="SecondaryColor">Código hexadecimal da cor secundária (ex.: #1E293B).</param>
/// <param name="AccentColor">Código hexadecimal da cor de destaque (ex.: #38BDF8).</param>
/// <param name="LogoUrl">URL ou caminho relativo da logomarca para tema claro.</param>
/// <param name="DarkLogoUrl">URL ou caminho relativo da logomarca para tema escuro (opcional).</param>
/// <param name="FaviconUrl">URL ou caminho relativo do favicon.</param>
/// <param name="CompanyName">Nome corporativo ou fantasia para exibição no rodapé e cabeçalho.</param>
/// <param name="ShowPoweredBy">Indica se a menção 'Powered by AdMetricsPro' deve ser exibida no rodapé.</param>
public record TenantBranding(
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string? LogoUrl = null,
    string? DarkLogoUrl = null,
    string? FaviconUrl = null,
    string? CompanyName = null,
    bool ShowPoweredBy = true)
{
    /// <summary>
    /// Configuração padrão institucional do AdMetricsPro.
    /// </summary>
    public static TenantBranding Default => new(
        PrimaryColor: "#2563EB",
        SecondaryColor: "#0F172A",
        AccentColor: "#38BDF8",
        LogoUrl: "/images/admetricspro-logo.svg",
        DarkLogoUrl: "/images/admetricspro-logo-dark.svg",
        FaviconUrl: "/favicon.ico",
        CompanyName: "AdMetricsPro",
        ShowPoweredBy: true);

    /// <summary>
    /// Gera a string de declaração das propriedades customizadas CSS (CSS Variables) para injeção dinâmica no tema.
    /// </summary>
    /// <returns>Bloco de estilos formatado com as variáveis CSS de marca.</returns>
    public string ToCssVariables()
    {
        return $"--tenant-primary: {PrimaryColor}; --tenant-secondary: {SecondaryColor}; --tenant-accent: {AccentColor};";
    }
}
