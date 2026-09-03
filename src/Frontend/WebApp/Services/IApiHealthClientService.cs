using BuildingBlocks.Domain.Primitives;
using Master.Application.Integrations.DTOs;
using Master.Domain.Integrations;

namespace WebApp.Services;

/// <summary>
/// Contrato do serviço cliente para o Hub de Monitoramento de APIs e Rate Limits no frontend Blazor.
/// </summary>
public interface IApiHealthClientService
{
    /// <summary>
    /// Obtém o resumo consolidado de cotas de APIs e saúde de conexões de inquilinos.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo a visão geral de saúde das APIs.</returns>
    Task<Result<ApiHealthOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a listagem filtrada de conexões de APIs de inquilinos.
    /// </summary>
    /// <param name="platform">Filtro opcional por plataforma.</param>
    /// <param name="status">Filtro opcional por status do token.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de conexões de APIs.</returns>
    Task<Result<IReadOnlyList<TenantApiConnectionDto>>> GetConnectionsAsync(
        AdPlatform? platform = null,
        ApiConnectionStatus? status = null,
        CancellationToken cancellationToken = default);
}
