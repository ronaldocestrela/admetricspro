using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Application.Tenants.Commands.ReactivateTenant;
using Master.Application.Tenants.Commands.SuspendTenant;
using Master.Application.Tenants.Queries.GetTenantDetails;
using Master.Domain.Tenants;
using MediatR;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Implementação do serviço de diretório 360º para consumo dos componentes do Blazor Server.
/// Orquestra chamadas in-memory via MediatR e repositório otimizado de leitura do módulo Master.
/// </summary>
public sealed class TenantDirectoryService : ITenantDirectoryService
{
    private readonly ISender _sender;
    private readonly ITenantReadOnlyRepository _readOnlyRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantDirectoryService"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory para envio de comandos e consultas.</param>
    /// <param name="readOnlyRepository">Repositório de leitura direta de tenants.</param>
    public TenantDirectoryService(ISender sender, ITenantReadOnlyRepository readOnlyRepository)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _readOnlyRepository = readOnlyRepository ?? throw new ArgumentNullException(nameof(readOnlyRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TenantDirectoryItemViewModel>>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _readOnlyRepository.GetAllAsync(cancellationToken);

        var viewModels = tenants.Select(t => new TenantDirectoryItemViewModel(
            Id: t.Id,
            CompanyName: t.CompanyName,
            Cnpj: t.Cnpj,
            Subdomain: t.Subdomain,
            Status: t.Status,
            Tier: t.Tier,
            SubscriptionExpiresAtUtc: t.SubscriptionExpiresAtUtc,
            CreatedAtUtc: t.CreatedAtUtc,
            WorkspacesCount: 1, // Valor default/estimado até Fase de Workspaces
            SunkAdSpend: 0m
        )).ToList();

        return Result<IReadOnlyList<TenantDirectoryItemViewModel>>.Success(viewModels);
    }

    /// <inheritdoc />
    public async Task<Result<Tenant360DetailsViewModel>> GetTenant360DetailsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return Result<Tenant360DetailsViewModel>.Failure(Error.Validation("Tenant.InvalidId", "O identificador do tenant não pode ser vazio."));
        }

        var result = await _sender.Send(new GetTenantDetailsQuery(new TenantId(tenantId)), cancellationToken);
        if (result.IsFailure)
        {
            return Result<Tenant360DetailsViewModel>.Failure(result.Error);
        }

        var details = result.Value;
        var viewModel = new Tenant360DetailsViewModel(
            Id: details.Id,
            CompanyName: details.CompanyName,
            Cnpj: details.Cnpj,
            Subdomain: details.Subdomain,
            CustomDomain: null,
            Status: details.Status,
            Tier: details.Tier,
            SubscriptionExpiresAtUtc: details.SubscriptionExpiresAtUtc,
            CreatedAtUtc: details.CreatedAtUtc,
            WorkspacesCount: 1,
            SunkAdSpend: 0m,
            ActiveIntegrationsCount: 0,
            TotalCampaignsCount: 0
        );

        return Result<Tenant360DetailsViewModel>.Success(viewModel);
    }

    /// <inheritdoc />
    public async Task<Result> SuspendTenantAsync(Guid tenantId, string reason, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Tenant.InvalidId", "O identificador do tenant não pode ser vazio."));
        }

        var command = new SuspendTenantCommand(new TenantId(tenantId), reason);
        return await _sender.Send(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> ReactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Tenant.InvalidId", "O identificador do tenant não pode ser vazio."));
        }

        var command = new ReactivateTenantCommand(new TenantId(tenantId));
        return await _sender.Send(command, cancellationToken);
    }
}
