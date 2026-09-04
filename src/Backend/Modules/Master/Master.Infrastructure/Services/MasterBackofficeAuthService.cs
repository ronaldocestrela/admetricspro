using BuildingBlocks.Domain.Primitives;
using Master.Application.Auditing;
using Master.Application.Users.DTOs;
using Master.Application.Users.Services;
using Master.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Master.Infrastructure.Services;

/// <summary>
/// Implementação do serviço de autenticação corporativa de operadores do Backoffice utilizando ASP.NET Core Identity.
/// </summary>
public sealed class MasterBackofficeAuthService : IBackofficeAuthService
{
    private readonly UserManager<MasterUser> _userManager;
    private readonly IMasterAuditService _auditService;
    private readonly ILogger<MasterBackofficeAuthService> _logger;

    /// <summary>
    /// Inicializa uma nova instância do serviço de autenticação do Backoffice.
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity.</param>
    /// <param name="auditService">Serviço de auditoria imutável do catálogo Master.</param>
    /// <param name="logger">Mecanismo de log estruturado.</param>
    public MasterBackofficeAuthService(
        UserManager<MasterUser> userManager,
        IMasterAuditService auditService,
        ILogger<MasterBackofficeAuthService> logger)
    {
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticatedBackofficeUserDto>> AuthenticateAsync(
        string email,
        string password,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Result<AuthenticatedBackofficeUserDto>.Failure(
                Error.Validation("Auth.InvalidInput", "E-mail e senha são obrigatórios."));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            _logger.LogWarning("Tentativa de login para e-mail inexistente no Backoffice: {Email} a partir do IP {IpAddress}", email, ipAddress ?? "N/A");
            return Result<AuthenticatedBackofficeUserDto>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha incorretos."));
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Tentativa de login em conta inativa/bloqueada no Backoffice: {Email}", email);
            return Result<AuthenticatedBackofficeUserDto>.Failure(
                Error.Unauthorized("Auth.AccountInactive", "Esta conta de operador está desativada."));
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            _logger.LogWarning("Falha de senha para operador do Backoffice: {Email}", email);

            await _auditService.RecordAsync(
                action: "BackofficeLoginFailed",
                resource: "Users",
                resourceId: user.Id.ToString(),
                details: $"Tentativa de autenticação com senha inválida para {email}",
                tenantId: null,
                ipAddress: ipAddress,
                additionalTags: new[] { "security", "auth_failure", "backoffice" },
                cancellationToken: cancellationToken);

            return Result<AuthenticatedBackofficeUserDto>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha incorretos."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.RecordLoginSuccess();
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        await _auditService.RecordAsync(
            action: "BackofficeLoginSuccess",
            resource: "Users",
            resourceId: user.Id.ToString(),
            details: $"Operador {user.FullName} ({email}) autenticado com sucesso no Backoffice.",
            tenantId: null,
            ipAddress: ipAddress,
            additionalTags: new[] { "security", "auth_success", "backoffice" },
            cancellationToken: cancellationToken);

        var userDto = new AuthenticatedBackofficeUserDto(
            user.Id,
            user.Email ?? email,
            user.FullName,
            roles.ToList(),
            user.LastLoginAtUtc);

        return Result<AuthenticatedBackofficeUserDto>.Success(userDto);
    }
}
