namespace Tenants.Application.Auth.DTOs;

/// <summary>
/// Dados públicos consolidados de identificação e identidade visual de um inquilino (White-Label).
/// Utilizado para renderização contextual da tela de autenticação antes do envio de credenciais.
/// </summary>
/// <param name="TenantId">Identificador único do inquilino no catálogo Master.</param>
/// <param name="CompanyName">Nome empresarial ou razão social da organização.</param>
/// <param name="Subdomain">Subdomínio exclusivo do inquilino.</param>
/// <param name="CustomDomain">Domínio personalizado mapeado via CNAME, se houver.</param>
/// <param name="PrimaryColor">Código hexadecimal da cor primária da marca (ex: #1E40AF).</param>
/// <param name="SecondaryColor">Código hexadecimal da cor secundária da marca (ex: #F59E0B).</param>
/// <param name="LogoUrl">URL opcional do logotipo corporativo.</param>
/// <param name="IsActive">Indica se o inquilino encontra-se com acesso liberado e adimplente.</param>
public sealed record TenantPublicBrandingDto(
    Guid TenantId,
    string CompanyName,
    string Subdomain,
    string? CustomDomain,
    string? PrimaryColor,
    string? SecondaryColor,
    string? LogoUrl,
    bool IsActive);
