namespace Tenants.Application.Users.DTOs;

/// <summary>
/// Resultado da operação de convite de colaborador.
/// </summary>
/// <param name="UserId">Identificador do colaborador criado.</param>
/// <param name="EmailDispatched">Indica se o e-mail transacional foi enviado com sucesso.</param>
public sealed record InviteTenantUserResultDto(Guid UserId, bool EmailDispatched);