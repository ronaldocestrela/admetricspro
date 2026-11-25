using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace WebApp.Services.Jobs;

/// <summary>
/// Cliente HTTP para consulta de jobs assíncronos iniciados pelo usuário.
/// </summary>
public interface IUserInitiatedJobClient
{
    /// <summary>
    /// Consulta o status atual de um job.
    /// </summary>
    /// <param name="workspaceId">Workspace.</param>
    /// <param name="jobId">Identificador do job.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>DTO do job.</returns>
    Task<Result<UserInitiatedJobDto>> GetJobAsync(
        Guid workspaceId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aguarda até o job atingir estado terminal ou timeout.
    /// </summary>
  Task<Result<UserInitiatedJobDto>> WaitForTerminalAsync(
        Guid workspaceId,
        Guid jobId,
        TimeSpan pollInterval,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}