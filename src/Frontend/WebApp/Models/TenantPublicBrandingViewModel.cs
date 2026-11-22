namespace WebApp.Models;

/// <summary>
/// Modelo de visualização com os dados públicos de marca e cores do inquilino para apresentação na tela de login.
/// </summary>
/// <param name="TenantId">Identificador único do inquilino.</param>
/// <param name="CompanyName">Nome empresarial ou marca da agência.</param>
/// <param name="Subdomain">Subdomínio exclusivo do inquilino.</param>
/// <param name="CustomDomain">Domínio CNAME personalizado, se houver.</param>
/// <param name="PrimaryColor">Código hexadecimal da cor primária da agência.</param>
/// <param name="SecondaryColor">Código hexadecimal da cor secundária da agência.</param>
/// <param name="LogoUrl">URL do logotipo corporativo.</param>
/// <param name="IsActive">Indica se a conta está ativa.</param>
public sealed record TenantPublicBrandingViewModel(
    Guid TenantId,
    string CompanyName,
    string Subdomain,
    string? CustomDomain,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LogoUrl,
    bool IsActive)
{
    /// <summary>
    /// Configuração padrão institucional do AdMetricsPro para fallbacks.
    /// </summary>
    public static TenantPublicBrandingViewModel Default => new(
        TenantId: Guid.Empty,
        CompanyName: "AdMetricsPro",
        Subdomain: "default",
        CustomDomain: null,
        PrimaryColor: "#0131CD",
        SecondaryColor: "#060D26",
        LogoUrl: null,
        IsActive: true);

    /// <summary>
    /// Gera a string de estilo CSS inline com as variáveis CSS de cor da agência.
    /// </summary>
    /// <returns>Bloco CSS formatado com as variáveis de marca.</returns>
    public string ToCssVariables()
    {
        var primary = !string.IsNullOrWhiteSpace(PrimaryColor) ? PrimaryColor : Default.PrimaryColor;
        var secondary = !string.IsNullOrWhiteSpace(SecondaryColor) ? SecondaryColor : Default.SecondaryColor;
        return $"--tenant-primary: {primary}; --tenant-primary-color: {primary}; --tenant-secondary: {secondary}; --tenant-secondary-color: {secondary};";
    }
}
