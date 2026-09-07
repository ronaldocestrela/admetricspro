using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Users.Commands.InviteTenantUser;

/// <summary>
/// Comando para cadastrar/convidar um colaborador para a agência no banco dedicado do inquilino.
/// </summary>
/// <param name="FullName">Nome completo do colaborador.</param>
/// <param name="Email">Endereço de e-mail corporativo.</param>
/// <param name="Role">Papel de governança (ex: MediaManager, Analyst).</param>
/// <param name="PhoneNumber">Telefone opcional de contato.</param>
public sealed record InviteTenantUserCommand(
    string FullName,
    string Email,
    string Role,
    string? PhoneNumber = null) : ICommand<Guid>;
