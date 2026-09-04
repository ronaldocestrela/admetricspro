using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Users.DTOs;
using Master.Application.Users.Services;

namespace Master.Application.Users.Commands.AuthenticateBackofficeUser;

/// <summary>
/// Manipulador do comando de autenticação de operadores do console Backoffice.
/// </summary>
public sealed class AuthenticateBackofficeUserCommandHandler : ICommandHandler<AuthenticateBackofficeUserCommand, AuthenticatedBackofficeUserDto>
{
    private readonly IBackofficeAuthService _authService;

    /// <summary>
    /// Inicializa uma nova instância do manipulador de autenticação com o serviço de segurança.
    /// </summary>
    /// <param name="authService">Instância do serviço de autenticação do Backoffice.</param>
    public AuthenticateBackofficeUserCommandHandler(IBackofficeAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Processa a autenticação do operador e retorna o DTO do usuário autenticado ou falha tipada.
    /// </summary>
    /// <param name="command">Comando contendo credenciais de login e dados de conexão.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição assíncrona.</param>
    /// <returns>Resultado contendo os dados do operador logado ou erro de autorização/validação.</returns>
    public async Task<Result<AuthenticatedBackofficeUserDto>> Handle(
        AuthenticateBackofficeUserCommand command,
        CancellationToken cancellationToken)
    {
        return await _authService.AuthenticateAsync(
            command.Email,
            command.Password,
            command.IpAddress,
            cancellationToken);
    }
}
