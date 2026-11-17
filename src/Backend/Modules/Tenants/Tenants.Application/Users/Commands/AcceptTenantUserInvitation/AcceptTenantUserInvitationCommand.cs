using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Users.Commands.AcceptTenantUserInvitation;

/// <summary>
/// Comando público para aceitar um convite e definir a senha de acesso.
/// </summary>
/// <param name="Token">Token opaco do convite.</param>
/// <param name="Subdomain">Subdomínio da agência.</param>
/// <param name="Password">Senha escolhida pelo colaborador.</param>
/// <param name="ConfirmPassword">Confirmação da senha.</param>
public sealed record AcceptTenantUserInvitationCommand(
    string Token,
    string Subdomain,
    string Password,
    string ConfirmPassword) : ICommand;