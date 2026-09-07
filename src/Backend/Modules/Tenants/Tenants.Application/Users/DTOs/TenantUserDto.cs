namespace Tenants.Application.Users.DTOs;

/// <summary>
/// Objeto de transferência com os dados de perfil de um colaborador do inquilino.
/// </summary>
/// <param name="Id">Identificador único do colaborador no banco do inquilino.</param>
/// <param name="FullName">Nome completo do colaborador.</param>
/// <param name="Email">Endereço de e-mail corporativo.</param>
/// <param name="PhoneNumber">Telefone opcional.</param>
/// <param name="Role">Papel de governança (ex: Owner, Admin, MediaManager, Analyst).</param>
/// <param name="IsActive">Status ativo da conta.</param>
/// <param name="CreatedAtUtc">Data de cadastro.</param>
public sealed record TenantUserDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc);
