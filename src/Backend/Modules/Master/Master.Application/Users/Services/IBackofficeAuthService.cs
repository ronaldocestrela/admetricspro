using BuildingBlocks.Domain.Primitives;
using Master.Application.Users.DTOs;

namespace Master.Application.Users.Services;

/// <summary>
/// Contrato de serviço para autenticação, validação de credenciais e auditoria de operadores do Backoffice.
/// </summary>
public interface IBackofficeAuthService
{
    /// <summary>
    /// Efetua a validação de credenciais de login de um operador do Backoffice.
    /// </summary>
    /// <param name="email">E-mail de acesso corporativo.</param>
    /// <param name="password">Senha em texto plano a ser validada contra o hash seguro.</param>
    /// <param name="ipAddress">Endereço IP de origem da requisição para fins de auditoria de segurança.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Resultado com o DTO do usuário autenticado ou erro tipado de negócio/segurança.</returns>
    Task<Result<AuthenticatedBackofficeUserDto>> AuthenticateAsync(
        string email,
        string password,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
