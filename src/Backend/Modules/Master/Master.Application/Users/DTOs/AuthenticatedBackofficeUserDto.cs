namespace Master.Application.Users.DTOs;

/// <summary>
/// Objeto de transferência de dados que representa um usuário autenticado no Backoffice.
/// </summary>
/// <param name="Id">Identificador único do operador no catálogo Master.</param>
/// <param name="Email">Endereço de e-mail corporativo utilizado para login.</param>
/// <param name="FullName">Nome completo do operador.</param>
/// <param name="Roles">Coleção de papéis/permissões atribuídos ao operador.</param>
/// <param name="LastLoginAtUtc">Timestamp UTC do último login registrado.</param>
public sealed record AuthenticatedBackofficeUserDto(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles,
    DateTime? LastLoginAtUtc);
