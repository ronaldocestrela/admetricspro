using BuildingBlocks.Domain.Primitives;
using BackofficeApp.Models;

namespace BackofficeApp.Services;

/// <summary>
/// Contrato de serviço do Frontend Blazor Server para consulta ao Diretório 360º e operações de ciclo de vida de tenants.
/// Todas as operações utilizam o padrão estrito Result para tratamento desacoplado de falhas.
/// </summary>
public interface ITenantDirectoryService
{
    /// <summary>
    /// Obtém a listagem completa dos tenants cadastrados no catálogo para exibição no grid gerencial.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado contendo a lista imutável de itens do diretório ou falha de negócio.</returns>
    Task<Result<IReadOnlyList<TenantDirectoryItemViewModel>>> GetTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a ficha completa 360º consolidada de um tenant, incluindo dados fiscais, contratuais e operacionais.
    /// </summary>
    /// <param name="tenantId">Identificador único do tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado contendo a ficha 360º detalhada ou falha de não localização.</returns>
    Task<Result<Tenant360DetailsViewModel>> GetTenant360DetailsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa a suspensão forçada de um tenant fornecendo justificativa formal de auditoria.
    /// </summary>
    /// <param name="tenantId">Identificador único do tenant.</param>
    /// <param name="reason">Motivo formal para a suspensão.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado de sucesso ou falha da operação.</returns>
    Task<Result> SuspendTenantAsync(Guid tenantId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restaura e reativa um tenant previamente suspenso.
    /// </summary>
    /// <param name="tenantId">Identificador único do tenant.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Resultado de sucesso ou falha da operação.</returns>
    Task<Result> ReactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
