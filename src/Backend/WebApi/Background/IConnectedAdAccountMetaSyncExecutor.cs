using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Integrations.DTOs;

namespace WebApi.Background;

/// <summary>
/// Executa sincronização Meta de leitura (hierarquia + métricas) para uma conta conectada.
/// </summary>
public interface IConnectedAdAccountMetaSyncExecutor
{
    /// <summary>
    /// Sincroniza hierarquia e métricas com journal Meta, usando o lookback configurado.
    /// </summary>
    /// <param name="tenantId">Tenant já contextualizado.</param>
    /// <param name="account">Conta conectada elegível.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Sucesso ou falha de negócio/transitória sem exceção.</returns>
    Task<Result> ExecuteAsync(Guid tenantId, ConnectedAdAccountDto account, CancellationToken cancellationToken = default);
}