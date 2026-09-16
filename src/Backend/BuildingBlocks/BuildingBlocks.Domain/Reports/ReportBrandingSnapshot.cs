namespace BuildingBlocks.Domain.Reports;

/// <summary>
/// Instantâneo imutável dos dados visuais e institucionais de White-Label da agência no momento da geração do relatório.
/// </summary>
/// <param name="PrimaryColor">Cor primária em formato hexadecimal (ex: #2563EB).</param>
/// <param name="SecondaryColor">Cor secundária em formato hexadecimal (ex: #0F172A).</param>
/// <param name="LightLogoUrl">URL pública da logomarca para fundos claros.</param>
/// <param name="DarkLogoUrl">URL pública da logomarca para fundos escuros.</param>
/// <param name="FaviconUrl">URL pública do favicon.</param>
/// <param name="AgencyName">Nome institucional da agência ou inquilino.</param>
/// <param name="SupportEmail">E-mail corporativo de contato/suporte da agência.</param>
/// <param name="SupportPhone">Telefone ou WhatsApp de atendimento da agência.</param>
/// <param name="CustomDomain">Domínio CNAME customizado da agência (ex: relatorios.agenciaalfa.com.br).</param>
public sealed record ReportBrandingSnapshot(
    string PrimaryColor,
    string SecondaryColor,
    string? LightLogoUrl,
    string? DarkLogoUrl,
    string? FaviconUrl,
    string AgencyName,
    string? SupportEmail,
    string? SupportPhone,
    string? CustomDomain)
{
    /// <summary>
    /// Retorna um snapshot padrão neutro caso a agência ainda não tenha customizado o branding.
    /// </summary>
    public static ReportBrandingSnapshot Default => new(
        PrimaryColor: "#2563EB",
        SecondaryColor: "#0F172A",
        LightLogoUrl: null,
        DarkLogoUrl: null,
        FaviconUrl: null,
        AgencyName: "Agência de Performance",
        SupportEmail: null,
        SupportPhone: null,
        CustomDomain: null);
}
