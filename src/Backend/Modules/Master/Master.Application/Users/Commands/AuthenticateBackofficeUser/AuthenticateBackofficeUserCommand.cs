using BuildingBlocks.Application.Messaging;
using Master.Application.Users.DTOs;

namespace Master.Application.Users.Commands.AuthenticateBackofficeUser;

/// <summary>
/// Comando de autenticação para login de operadores no console de administração Backoffice.
/// </summary>
/// <param name="Email">E-mail corporativo cadastrado.</param>
/// <param name="Password">Senha de acesso em texto plano.</param>
/// <param name="IpAddress">Endereço IP de origem da requisição.</param>
public sealed record AuthenticateBackofficeUserCommand(
    string Email,
    string Password,
    string? IpAddress = null) : ICommand<AuthenticatedBackofficeUserDto>;
