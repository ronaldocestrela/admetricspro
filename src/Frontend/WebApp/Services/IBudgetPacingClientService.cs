using Automations.Application.Pacing.DTOs;
using BuildingBlocks.Domain.Primitives;

namespace WebApp.Services;

/// <summary>
/// Contrato do cliente HTTP para consumo das funcionalidades de Pacing e projeção de verba na Web API.
/// </summary>
public interface IBudgetPacingClientService
{
    /// <summary>
    /// Consulta o pacing consolidado e a previsão de fechamento de mês para um workspace específico.
    /// </summary>
    /// <param name="workspaceId">Identificador único do workspace.</param>
    /// <param name="year">Ano de referência opcional.</param>
    /// <param name="month">Mês de referência opcional.</param>
    /// <param name="asOfDateUtc">Data de corte opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com dados de pacing do workspace.</returns>
    Task<Result<WorkspaceBudgetPacingDto>> GetWorkspacePacingAsync(
        Guid workspaceId,
        int? year = null,
        int? month = null,
        DateTime? asOfDateUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta a visão executiva consolidada de pacing da carteira de clientes do inquilino.
    /// </summary>
    /// <param name="squadId">Filtro opcional por Squad.</param>
    /// <param name="year">Ano de referência opcional.</param>
    /// <param name="month">Mês de referência opcional.</param>
    /// <param name="asOfDateUtc">Data de corte opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com o sumário executivo da carteira.</returns>
    Task<Result<PortfolioPacingSummaryDto>> GetPortfolioPacingAsync(
        Guid? squadId = null,
        int? year = null,
        int? month = null,
        DateTime? asOfDateUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa uma simulação dinâmica sob demanda de projeção e pacing na Web API.
    /// </summary>
    /// <param name="request">Parâmetros de entrada da simulação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da simulação.</returns>
    Task<Result<WorkspaceBudgetPacingDto>> SimulatePacingAsync(
        SimulatePacingRequestDto request,
        CancellationToken cancellationToken = default);
}
