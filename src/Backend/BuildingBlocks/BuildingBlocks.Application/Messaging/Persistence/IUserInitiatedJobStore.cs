using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging.Persistence;

/// <summary>
/// Leitura e atualização de jobs iniciados pelo usuário no banco do tenant.
/// </summary>
public interface IUserInitiatedJobStore
{
    /// <summary>
    /// Localiza um job pelo identificador e workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace de escopo.</param>
    /// <param name="jobId">Identificador do job.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Job ou nulo quando ausente.</returns>
    Task<UserInitiatedJob?> GetByIdAsync(
        Guid workspaceId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Localiza um job apenas pelo identificador (consumidor com tenant já restaurado).
    /// </summary>
    /// <param name="jobId">Identificador do job.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Job ou nulo.</returns>
    Task<UserInitiatedJob?> GetByIdForExecutionAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um job ao change tracker do contexto atual.
    /// </summary>
    /// <param name="job">Job criado.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> AddAsync(UserInitiatedJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste alterações de estado de um job já rastreado.
    /// </summary>
    /// <param name="job">Job atualizado.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> UpdateAsync(UserInitiatedJob job, CancellationToken cancellationToken = default);
}