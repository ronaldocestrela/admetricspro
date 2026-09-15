using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.ConfigureTenantCustomDomain;

/// <summary>
/// Manipulador responsável por validar requisitos de plano, unicidade e registrar o domínio CNAME no catálogo MasterDb.
/// </summary>
public sealed class ConfigureTenantCustomDomainCommandHandler : ICommandHandler<ConfigureTenantCustomDomainCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ConfigureTenantCustomDomainCommandHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório do catálogo de inquilinos.</param>
    /// <param name="planRepository">Repositório de planos de assinatura.</param>
    /// <param name="unitOfWork">Unidade de trabalho do catálogo MasterDb.</param>
    public ConfigureTenantCustomDomainCommandHandler(
        ITenantRepository tenantRepository,
        IPlanRepository planRepository,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _planRepository = planRepository ?? throw new ArgumentNullException(nameof(planRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ConfigureTenantCustomDomainCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.CustomDomain))
        {
            return Result.Failure(Error.Validation("Tenant.InvalidCustomDomain", "O domínio personalizado não pode ser vazio."));
        }

        var normalized = command.CustomDomain.Trim().ToLowerInvariant();

        if (normalized.Contains("://") || normalized.Contains('/') || normalized.Contains(':') || normalized.Contains(' ') || !normalized.Contains('.'))
        {
            return Result.Failure(Error.Validation(
                "Tenant.InvalidCustomDomain",
                "O domínio personalizado deve ser um FQDN válido sem protocolos (http/https), portas, caminhos ou espaços."));
        }

        var tenant = await _tenantRepository.GetByIdAsync(new TenantId(command.TenantId), cancellationToken);
        if (tenant is null)
        {
            return Result.Failure(Error.NotFound("Tenant.NotFound", "Inquilino não localizado no catálogo central."));
        }

        var plan = await _planRepository.GetByTierAsync(tenant.Tier, cancellationToken);
        if (plan is null || !plan.Features.HasCustomCname)
        {
            return Result.Failure(Error.Forbidden(
                "Tenant.PlanLacksCustomCname",
                "O plano contratado não contempla a utilização de domínio CNAME personalizado."));
        }

        var existingWithDomain = await _tenantRepository.GetByCustomDomainAsync(normalized, cancellationToken);
        if (existingWithDomain is not null && existingWithDomain.Id != tenant.Id)
        {
            return Result.Failure(Error.Conflict(
                "Tenant.CustomDomainConflict",
                $"O domínio '{normalized}' já está vinculado a outra organização."));
        }

        var updateResult = tenant.UpdateBranding(tenant.PrimaryColor, tenant.SecondaryColor, normalized);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        _tenantRepository.Update(tenant);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
