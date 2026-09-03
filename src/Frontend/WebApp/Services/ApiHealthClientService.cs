using BuildingBlocks.Domain.Primitives;
using Master.Application.Integrations.DTOs;
using Master.Application.Integrations.Queries.GetApiHealthOverview;
using Master.Application.Integrations.Queries.GetTenantApiConnections;
using Master.Domain.Integrations;
using MediatR;

namespace WebApp.Services;

/// <summary>
/// Implementação do serviço cliente para monitoramento de saúde de APIs e cotas no Blazor Server.
/// Utiliza despacho in-memory via MediatR para consultar diretamente a camada de aplicação.
/// </summary>
public sealed class ApiHealthClientService : IApiHealthClientService
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ApiHealthClientService"/>.
    /// </summary>
    /// <param name="sender">Mediador de consultas in-memory.</param>
    public ApiHealthClientService(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public async Task<Result<ApiHealthOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        return await _sender.Send(new GetApiHealthOverviewQuery(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TenantApiConnectionDto>>> GetConnectionsAsync(
        AdPlatform? platform = null,
        ApiConnectionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return await _sender.Send(
            new GetTenantApiConnectionsQuery(platform, status, PageNumber: 1, PageSize: 100),
            cancellationToken);
    }
}
