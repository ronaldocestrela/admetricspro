using BuildingBlocks.Domain.Messaging;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Representação HTTP do status de um job iniciado pelo usuário.
/// </summary>
/// <param name="JobId">Identificador do job.</param>
/// <param name="WorkspaceId">Workspace do pedido.</param>
/// <param name="Kind">Tipo de trabalho.</param>
/// <param name="Status">Status atual.</param>
/// <param name="ResultJson">JSON de resultado quando concluído.</param>
/// <param name="ErrorCode">Código de erro quando falhou.</param>
/// <param name="ErrorDescription">Descrição do erro quando falhou.</param>
/// <param name="CreatedAtUtc">Criação em UTC.</param>
/// <param name="StartedAtUtc">Início da execução em UTC.</param>
/// <param name="CompletedAtUtc">Conclusão em UTC.</param>
public sealed record UserInitiatedJobDto(
    Guid JobId,
    Guid WorkspaceId,
    UserInitiatedJobKind Kind,
    UserInitiatedJobStatus Status,
    string? ResultJson,
    string? ErrorCode,
    string? ErrorDescription,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc)
{
    /// <summary>
    /// Converte a entidade persistida em DTO de API.
    /// </summary>
    /// <param name="job">Job do banco do tenant.</param>
    /// <returns>DTO serializável.</returns>
    public static UserInitiatedJobDto FromEntity(UserInitiatedJob job) =>
        new(
            job.Id,
            job.WorkspaceId,
            job.Kind,
            job.Status,
            job.ResultJson,
            job.ErrorCode,
            job.ErrorDescription,
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc);
}