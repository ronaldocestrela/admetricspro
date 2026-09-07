using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tenants.Application.Auth.DTOs;
using Tenants.Application.Auth.Services;

namespace Tenants.Infrastructure.Auth;

/// <summary>
/// Implementação do serviço de autenticação e governança de inquilinos operacionais (<see cref="ITenantAuthService"/>).
/// Orquestra a resolução contextual do inquilino, validação de segurança e emissão de tokens de sessão.
/// </summary>
public sealed class TenantAuthService : ITenantAuthService
{
    private const int DefaultTokenLifetimeSeconds = 28800; // 8 horas

    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ITenantDbContextFactory<TenantDbContext> _tenantDbContextFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantTokenService _tokenService;
    private readonly ILogger<TenantAuthService> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantAuthService"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de inquilinos do catálogo Master.</param>
    /// <param name="tenantContextAccessor">Acessor do contexto de inquilino ativo da requisição HTTP.</param>
    /// <param name="tenantDbContextFactory">Fábrica dinâmica de contextos isolados por inquilino.</param>
    /// <param name="passwordHasher">Serviço de verificação e hashing seguro de senhas.</param>
    /// <param name="tokenService">Serviço de emissão e assinatura de tokens JWT de inquilino.</param>
    /// <param name="logger">Mecanismo de log estruturado.</param>
    public TenantAuthService(
        ITenantRepository tenantRepository,
        ITenantContextAccessor tenantContextAccessor,
        ITenantDbContextFactory<TenantDbContext> tenantDbContextFactory,
        IPasswordHasher passwordHasher,
        ITenantTokenService tokenService,
        ILogger<TenantAuthService> logger)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _tenantDbContextFactory = tenantDbContextFactory ?? throw new ArgumentNullException(nameof(tenantDbContextFactory));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticatedTenantUserDto>> AuthenticateAsync(
        string email,
        string password,
        string? subdomain = null,
        Guid? tenantId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Result<AuthenticatedTenantUserDto>.Failure(
                Error.Validation("Auth.InvalidInput", "E-mail e senha são obrigatórios."));
        }

        // 1. Resolução da Identidade do Inquilino
        var (resolvedSubdomain, resolvedTenantId) = ResolveTenantIdentity(subdomain, tenantId);

        if (string.IsNullOrWhiteSpace(resolvedSubdomain) && (!resolvedTenantId.HasValue || resolvedTenantId.Value == Guid.Empty))
        {
            return Result<AuthenticatedTenantUserDto>.Failure(
                Error.Validation("Tenant.IdentifierRequired", "O inquilino deve ser identificado via subdomínio, cabeçalho ou parâmetro de requisição."));
        }

        // 2. Consulta do Inquilino no Catálogo Master
        Tenant? tenant = null;
        if (!string.IsNullOrWhiteSpace(resolvedSubdomain))
        {
            tenant = await _tenantRepository.GetBySubdomainAsync(resolvedSubdomain.Trim().ToLowerInvariant(), cancellationToken);
        }
        else if (resolvedTenantId.HasValue)
        {
            tenant = await _tenantRepository.GetByIdAsync(new TenantId(resolvedTenantId.Value), cancellationToken);
        }

        if (tenant is null)
        {
            _logger.LogWarning("Tentativa de login para inquilino não localizado. Subdomain: {Subdomain}, TenantId: {TenantId}",
                resolvedSubdomain, resolvedTenantId);
            return Result<AuthenticatedTenantUserDto>.Failure(
                Error.NotFound("Tenant.NotFound", "Inquilino não encontrado no catálogo Master."));
        }

        // 3. Validação do Status do Inquilino
        if (tenant.Status == TenantStatus.Suspended || tenant.Status == TenantStatus.Cancelled)
        {
            _logger.LogWarning("Tentativa de login rejeitada: inquilino {CompanyName} está com status {Status}",
                tenant.CompanyName, tenant.Status);
            return Result<AuthenticatedTenantUserDto>.Failure(
                Error.Validation("Tenant.Inactive", $"O acesso deste inquilino está {tenant.Status}."));
        }

        // 4. Conexão ao Banco Dedicado do Inquilino
        var dbContextResult = await _tenantDbContextFactory.CreateDbContextAsync(tenant.Id.Value, cancellationToken);
        if (dbContextResult.IsFailure)
        {
            _logger.LogError("Falha ao instanciar banco dedicado para inquilino {TenantId}: {Error}",
                tenant.Id.Value, dbContextResult.Error.Description);
            return Result<AuthenticatedTenantUserDto>.Failure(dbContextResult.Error);
        }

        await using var tenantDbContext = dbContextResult.Value;

        // 5. Validação das Credenciais do Usuário
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await tenantDbContext.TenantUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Tentativa de login com usuário inexistente {Email} no tenant {Subdomain}",
                email, tenant.Subdomain);
            return Result<AuthenticatedTenantUserDto>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha incorretos."));
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Tentativa de login com usuário inativo {Email} no tenant {Subdomain}",
                email, tenant.Subdomain);
            return Result<AuthenticatedTenantUserDto>.Failure(
                Error.Unauthorized("Auth.AccountInactive", "Esta conta de usuário está desativada."));
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(user.PasswordHash, password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Senha incorreta informada para usuário {Email} no tenant {Subdomain}",
                email, tenant.Subdomain);
            return Result<AuthenticatedTenantUserDto>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha incorretos."));
        }

        // 6. Consulta de Branding White-Label
        var branding = await tenantDbContext.TenantBranding
            .FirstOrDefaultAsync(cancellationToken);

        TenantBrandingDto? brandingDto = branding is not null
            ? new TenantBrandingDto(
                tenant.CompanyName,
                branding.PrimaryColor,
                branding.SecondaryColor,
                branding.LightLogoUrl,
                branding.DarkLogoUrl,
                branding.FaviconUrl)
            : null;

        // 7. Emissão do Token JWT do Inquilino
        var tokenResult = _tokenService.GenerateToken(user, tenant.Id.Value, tenant.Subdomain);
        if (tokenResult.IsFailure)
        {
            _logger.LogError("Falha ao gerar token JWT para usuário {UserId} no tenant {TenantId}: {Error}",
                user.Id, tenant.Id.Value, tokenResult.Error.Description);
            return Result<AuthenticatedTenantUserDto>.Failure(tokenResult.Error);
        }

        _logger.LogInformation("Usuário {Email} ({Role}) autenticado com sucesso no tenant {Subdomain}",
            user.Email, user.Role, tenant.Subdomain);

        var dto = new AuthenticatedTenantUserDto(
            AccessToken: tokenResult.Value,
            TokenType: "Bearer",
            ExpiresIn: DefaultTokenLifetimeSeconds,
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role.ToString(),
            TenantId: tenant.Id.Value,
            Subdomain: tenant.Subdomain,
            Branding: brandingDto);

        return Result<AuthenticatedTenantUserDto>.Success(dto);
    }

    private (string? Subdomain, Guid? TenantId) ResolveTenantIdentity(string? subdomain, Guid? tenantId)
    {
        if (!string.IsNullOrWhiteSpace(subdomain))
        {
            return (subdomain.Trim().ToLowerInvariant(), null);
        }

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            return (null, tenantId.Value);
        }

        var context = _tenantContextAccessor.TenantContext;
        if (context is not null && context.IsResolved)
        {
            if (!string.IsNullOrWhiteSpace(context.Subdomain))
            {
                return (context.Subdomain.Trim().ToLowerInvariant(), null);
            }

            if (context.TenantId.HasValue && context.TenantId.Value != Guid.Empty)
            {
                return (null, context.TenantId.Value);
            }
        }

        return (null, null);
    }
}
