using BuildingBlocks.Domain.Primitives;
using Master.Application.Plans.DTOs;
using BackofficeApp.Models;

namespace BackofficeApp.Services;

/// <summary>
/// Contrato de serviço para governança e parametrização de planos de assinatura no frontend Blazor.
/// </summary>
public interface IPlanManagementService
{
    /// <summary>
    /// Lista todos os planos de assinatura disponíveis.
    /// </summary>
    /// <param name="includeInactive">Se verdadeiro, inclui planos inativos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo a lista de planos cadastrados.</returns>
    Task<Result<IReadOnlyList<PlanDto>>> GetPlansAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os dados de um plano específico pelo identificador.
    /// </summary>
    /// <param name="id">Identificador do plano.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo o plano encontrado ou nulo.</returns>
    Task<Result<PlanDto?>> GetPlanByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria um novo plano de assinatura no catálogo master.
    /// </summary>
    /// <param name="model">Dados preenchidos no formulário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo o identificador do plano criado ou falha de negócio.</returns>
    Task<Result<Guid>> CreatePlanAsync(PlanFormViewModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza as cotas, precificação e recursos de um plano de assinatura.
    /// </summary>
    /// <param name="model">Dados atualizados no formulário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> UpdatePlanAsync(PlanFormViewModel model, CancellationToken cancellationToken = default);
}
