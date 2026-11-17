using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Tenants.Application.Users.DTOs;
using Tenants.Application.Users.Services;

namespace Tenants.Infrastructure.Users;

/// <summary>
/// Implementação de <see cref="ITenantInvitationPublicGateway"/> com resolução via catálogo Master e banco dedicado.
/// </summary>
public sealed class TenantInvitationPublicGateway : ITenantInvitationPublicGateway
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantDbContextFactory<TenantDbContext> _tenantDbContextFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantInvitationTokenService _tokenService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantInvitationPublicGateway"/>.
    /// </summary>
    public TenantInvitationPublicGateway(
        ITenantRepository tenantRepository,
        ITenantDbContextFactory<TenantDbContext> tenantDbContextFactory,
        IPasswordHasher passwordHasher,
        ITenantInvitationTokenService tokenService)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _tenantDbContextFactory = tenantDbContextFactory ?? throw new ArgumentNullException(nameof(tenantDbContextFactory));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    /// <inheritdoc />
    public async Task<Result<TenantInvitationPreviewDto>> GetPreviewAsync(
        string token,
        string subdomain,
        CancellationToken cancellationToken = default)
    {
        var tenantResult = await ResolveTenantAsync(subdomain, cancellationToken);
        if (tenantResult.IsFailure)
        {
            return Result<TenantInvitationPreviewDto>.Failure(tenantResult.Error);
        }

        var tenant = tenantResult.Value;
        var dbContextResult = await _tenantDbContextFactory.CreateDbContextAsync(tenant.Id.Value, cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return Result<TenantInvitationPreviewDto>.Failure(dbContextResult.Error);
        }

        await using var dbContext = dbContextResult.Value;
        var lookup = await LoadOpenInvitationAsync(dbContext, token, cancellationToken);
        if (lookup.IsFailure)
        {
            return Result<TenantInvitationPreviewDto>.Failure(lookup.Error);
        }

        var (invitation, user) = lookup.Value;
        var dto = new TenantInvitationPreviewDto(
            user.FullName,
            user.Email,
            tenant.CompanyName,
            tenant.Subdomain,
            invitation.ExpiresAtUtc);

        return Result<TenantInvitationPreviewDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result> AcceptAsync(
        string token,
        string subdomain,
        string password,
        CancellationToken cancellationToken = default)
    {
        var tenantResult = await ResolveTenantAsync(subdomain, cancellationToken);
        if (tenantResult.IsFailure)
        {
            return Result.Failure(tenantResult.Error);
        }

        var tenant = tenantResult.Value;
        var dbContextResult = await _tenantDbContextFactory.CreateDbContextAsync(tenant.Id.Value, cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return Result.Failure(dbContextResult.Error);
        }

        await using var dbContext = dbContextResult.Value;
        var lookup = await LoadOpenInvitationAsync(dbContext, token, cancellationToken);
        if (lookup.IsFailure)
        {
            return Result.Failure(lookup.Error);
        }

        var (invitation, user) = lookup.Value;

        if (user.IsActive)
        {
            return Result.Failure(
                Error.Conflict("TenantUser.AlreadyActive", "Esta conta já está ativa."));
        }

        var acceptResult = invitation.Accept();
        if (acceptResult.IsFailure)
        {
            return acceptResult;
        }

        var passwordHash = _passwordHasher.HashPassword(password);
        var changePassword = user.ChangePasswordHash(passwordHash);
        if (changePassword.IsFailure)
        {
            return changePassword;
        }

        var activate = user.Activate();
        if (activate.IsFailure)
        {
            return activate;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<Tenant>> ResolveTenantAsync(string subdomain, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return Result<Tenant>.Failure(
                Error.Validation("TenantInvitation.InvalidInput", "Subdomínio é obrigatório."));
        }

        var tenant = await _tenantRepository.GetBySubdomainAsync(subdomain.Trim().ToLowerInvariant(), cancellationToken);
        if (tenant is null)
        {
            return Result<Tenant>.Failure(
                Error.NotFound("Tenant.NotFound", "Agência não encontrada para o subdomínio informado."));
        }

        if (tenant.Status is TenantStatus.Suspended or TenantStatus.Cancelled)
        {
            return Result<Tenant>.Failure(
                Error.Validation("Tenant.Inactive", "Esta agência não está aceitando novos acessos."));
        }

        return Result<Tenant>.Success(tenant);
    }

    private async Task<Result<(TenantUserInvitation Invitation, TenantUser User)>> LoadOpenInvitationAsync(
        TenantDbContext dbContext,
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<(TenantUserInvitation, TenantUser)>.Failure(
                Error.Validation("TenantInvitation.InvalidInput", "Token é obrigatório."));
        }

        var tokenHash = _tokenService.Hash(token);
        var invitation = await dbContext.TenantUserInvitations
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

        if (invitation is null || !invitation.IsOpen)
        {
            return Result<(TenantUserInvitation, TenantUser)>.Failure(
                Error.NotFound("TenantInvitation.NotFound", "Convite inválido, expirado ou já utilizado."));
        }

        var user = await dbContext.TenantUsers.FirstOrDefaultAsync(u => u.Id == invitation.UserId, cancellationToken);
        if (user is null)
        {
            return Result<(TenantUserInvitation, TenantUser)>.Failure(
                Error.NotFound("TenantUser.NotFound", "Colaborador do convite não foi encontrado."));
        }

        return Result<(TenantUserInvitation, TenantUser)>.Success((invitation, user));
    }
}