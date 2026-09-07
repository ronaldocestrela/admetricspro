namespace Tenants.Application.Auth.DTOs;

/// <summary>
/// Objeto de transferência com as credenciais, dados de perfil e metadados visuais do usuário de inquilino autenticado.
/// </summary>
/// <param name="AccessToken">Token JWT assinado digitalmente com as claims da sessão do inquilino.</param>
/// <param name="TokenType">Tipo do token emitido (padrão: "Bearer").</param>
/// <param name="ExpiresIn">Tempo de vida do token em segundos.</param>
/// <param name="UserId">Identificador único global do usuário dentro do banco dedicado do inquilino.</param>
/// <param name="Email">Endereço de e-mail corporativo autenticado.</param>
/// <param name="FullName">Nome completo do usuário.</param>
/// <param name="Role">Papel de governança do usuário (ex: Owner, Admin, MediaBuyer).</param>
/// <param name="TenantId">Identificador único global do inquilino no catálogo Master.</param>
/// <param name="Subdomain">Subdomínio exclusivo do inquilino (ex: "vanguarda").</param>
/// <param name="Branding">Informações de identidade visual e marca associadas ao inquilino.</param>
public sealed record AuthenticatedTenantUserDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid TenantId,
    string Subdomain,
    TenantBrandingDto? Branding);
