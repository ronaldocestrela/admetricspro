namespace Tenants.Application.Users.DTOs;

/// <summary>
/// Dados públicos exibidos na tela de aceite de convite.
/// </summary>
/// <param name="FullName">Nome do colaborador convidado.</param>
/// <param name="Email">E-mail do colaborador.</param>
/// <param name="AgencyName">Nome da agência.</param>
/// <param name="Subdomain">Subdomínio do inquilino.</param>
/// <param name="ExpiresAtUtc">Expiração do convite em UTC.</param>
public sealed record TenantInvitationPreviewDto(
    string FullName,
    string Email,
    string AgencyName,
    string Subdomain,
    DateTime ExpiresAtUtc);