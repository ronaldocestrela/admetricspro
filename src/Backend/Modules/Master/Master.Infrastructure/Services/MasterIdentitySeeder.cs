using BuildingBlocks.Domain.Primitives;
using Master.Application.Auditing;
using Master.Application.Users.Services;
using Master.Infrastructure.Configuration;
using Master.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Master.Infrastructure.Services;

/// <summary>
/// Provedor de seed e inicialização idempotente do Super Administrador do Backoffice via variáveis de ambiente (.env).
/// </summary>
public sealed class MasterIdentitySeeder : IMasterIdentitySeeder
{
    private readonly UserManager<MasterUser> _userManager;
    private readonly RoleManager<MasterRole> _roleManager;
    private readonly SuperAdminSeedOptions _options;
    private readonly IMasterAuditService _auditService;
    private readonly ILogger<MasterIdentitySeeder> _logger;

    /// <summary>
    /// Inicializa uma nova instância do inicializador de identidade do catálogo Master.
    /// </summary>
    /// <param name="userManager">Gerenciador de usuários do Identity.</param>
    /// <param name="roleManager">Gerenciador de perfis/roles do Identity.</param>
    /// <param name="options">Opções tipadas contendo credenciais lidas do .env.</param>
    /// <param name="auditService">Serviço de trilha de auditoria global.</param>
    /// <param name="logger">Mecanismo de logging estruturado.</param>
    public MasterIdentitySeeder(
        UserManager<MasterUser> userManager,
        RoleManager<MasterRole> roleManager,
        IOptions<SuperAdminSeedOptions> options,
        IMasterAuditService auditService,
        ILogger<MasterIdentitySeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _options = options.Value;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> SeedSuperAdminAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureRoleExistsAsync(MasterRole.SuperAdmin, "Administrador global com acesso irrestrito a todos os recursos da plataforma.");
            await EnsureRoleExistsAsync(MasterRole.SupportTechnician, "Técnico de suporte corporativo para diagnóstico e sessões de impersonation auditadas.");

            if (string.IsNullOrWhiteSpace(_options.Email) || string.IsNullOrWhiteSpace(_options.Password))
            {
                _logger.LogWarning("Credenciais de SuperAdmin ausentes ou vazias no arquivo .env. Seed ignorado.");
                return Result.Success();
            }

            var normalizedEmail = _options.Email.Trim().ToLowerInvariant();
            var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);

            if (existingUser is null)
            {
                _logger.LogInformation("Criando usuário Super Administrador inicial a partir das variáveis do .env: {Email}", _options.Email);

                var superAdmin = new MasterUser(normalizedEmail, _options.FullName)
                {
                    EmailConfirmed = true,
                    IsActive = true
                };

                var createResult = await _userManager.CreateAsync(superAdmin, _options.Password);
                if (!createResult.Succeeded)
                {
                    var errorDescriptions = string.Join("; ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    _logger.LogError("Falha ao criar o usuário Super Administrador inicial: {Errors}", errorDescriptions);
                    return Result.Failure(Error.Failure("Identity.SuperAdminCreationFailed", errorDescriptions));
                }

                var roleToAssign = string.IsNullOrWhiteSpace(_options.Role) ? MasterRole.SuperAdmin : _options.Role;
                await _userManager.AddToRoleAsync(superAdmin, roleToAssign);

                await _auditService.RecordAsync(
                    action: "SuperAdminUserSeeded",
                    resource: "Users",
                    resourceId: superAdmin.Id.ToString(),
                    details: $"Super Administrador inicial ({_options.Email}) provisionado com sucesso via seed do .env.",
                    tenantId: null,
                    ipAddress: "127.0.0.1",
                    additionalTags: new[] { "system", "seed", "identity", "superadmin" },
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Super Administrador inicial criado e vinculado à role '{Role}' com sucesso.", roleToAssign);
            }
            else
            {
                var roleToAssign = string.IsNullOrWhiteSpace(_options.Role) ? MasterRole.SuperAdmin : _options.Role;
                if (!await _userManager.IsInRoleAsync(existingUser, roleToAssign))
                {
                    await _userManager.AddToRoleAsync(existingUser, roleToAssign);
                    _logger.LogInformation("Vinculação da role '{Role}' restaurada para o Super Administrador existente: {Email}", roleToAssign, _options.Email);
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao executar o seed de identidade do Super Administrador.");
            return Result.Failure(Error.Failure("Identity.SeedException", ex.Message));
        }
    }

    private async Task EnsureRoleExistsAsync(string roleName, string description)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var role = new MasterRole(roleName, description);
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Falha ao criar role padrão '{RoleName}': {Errors}", roleName, errors);
            }
            else
            {
                _logger.LogInformation("Role padrão '{RoleName}' criada no catálogo Master.", roleName);
            }
        }
    }
}
