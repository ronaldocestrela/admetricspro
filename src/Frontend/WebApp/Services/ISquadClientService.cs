using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Squads.DTOs;
using Tenants.Application.Workspaces.DTOs;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Contrato do cliente HTTP para gestão de equipes internas (Squads) e governança de carteira de clientes no banco do inquilino.
/// </summary>
public interface ISquadClientService
{
    /// <summary>
    /// Cadastra um novo squad interno na agência.
    /// </summary>
    /// <param name="model">Dados do squad.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do squad criado ou falha semântica.</returns>
    Task<Result<Guid>> CreateSquadAsync(CreateSquadModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os squads cadastrados na agência com resumo de membros e clientes.
    /// </summary>
    /// <param name="activeOnly">Filtro opcional para retornar apenas squads ativos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista sumarizada de squads.</returns>
    Task<Result<IReadOnlyList<SquadSummaryDto>>> GetSquadsAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os detalhes completos de um squad, incluindo seus membros e workspaces alocados.
    /// </summary>
    /// <param name="id">Identificador único do squad.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados detalhados do squad ou falha tipada.</returns>
    Task<Result<SquadDetailsDto>> GetSquadByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza os dados cadastrais (nome e descrição) de um squad existente.
    /// </summary>
    /// <param name="id">Identificador do squad.</param>
    /// <param name="model">Novos dados cadastrais.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> UpdateSquadAsync(Guid id, UpdateSquadModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alterna o status operacional (ativo/inativo) de um squad.
    /// </summary>
    /// <param name="id">Identificador do squad.</param>
    /// <param name="isActive">Novo status.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> ToggleSquadStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um colaborador à equipe do squad.
    /// </summary>
    /// <param name="squadId">Identificador do squad.</param>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> AddSquadMemberAsync(Guid squadId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um colaborador da equipe do squad.
    /// </summary>
    /// <param name="squadId">Identificador do squad.</param>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> RemoveSquadMemberAsync(Guid squadId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aloca um cliente/workspace à carteira de atendimento do squad.
    /// </summary>
    /// <param name="squadId">Identificador do squad.</param>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> AssignSquadWorkspaceAsync(Guid squadId, Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um cliente/workspace da carteira de atendimento do squad.
    /// </summary>
    /// <param name="squadId">Identificador do squad.</param>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> UnassignSquadWorkspaceAsync(Guid squadId, Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta a carteira de clientes/workspaces acessíveis a um colaborador segundo as regras de isolamento.
    /// </summary>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de workspaces acessíveis.</returns>
    Task<Result<IReadOnlyList<WorkspaceDto>>> GetUserPortfolioAsync(Guid userId, CancellationToken cancellationToken = default);
}
