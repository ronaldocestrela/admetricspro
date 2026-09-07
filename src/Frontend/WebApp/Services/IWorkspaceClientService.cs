using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Workspaces.DTOs;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Contrato do cliente HTTP para gestão de clientes/workspaces no banco dedicado do inquilino.
/// </summary>
public interface IWorkspaceClientService
{
    /// <summary>
    /// Cadastra um novo cliente/workspace para a agência.
    /// </summary>
    /// <param name="model">Dados cadastrais e orçamento do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do workspace criado ou falha semântica.</returns>
    Task<Result<Guid>> CreateWorkspaceAsync(CreateWorkspaceModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os workspaces cadastrados no inquilino com filtro opcional por status ativo.
    /// </summary>
    /// <param name="activeOnly">Filtro opcional para retornar apenas workspaces ativos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de workspaces ou erro de comunicação.</returns>
    Task<Result<IReadOnlyList<WorkspaceDto>>> GetWorkspacesAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
}
