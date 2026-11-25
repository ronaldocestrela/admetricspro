using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Messaging;

/// <summary>
/// Registro de trabalho iniciado pelo usuário, durável no banco do tenant, com execução assíncrona via fila.
/// </summary>
public sealed class UserInitiatedJob : Entity<Guid>
{
    private UserInitiatedJob(
        Guid id,
        Guid tenantId,
        Guid workspaceId,
        UserInitiatedJobKind kind,
        UserInitiatedJobStatus status,
        Guid eventId,
        string parametersJson,
        string? resultJson,
        string? errorCode,
        string? errorDescription,
        DateTime createdAtUtc,
        DateTime? startedAtUtc,
        DateTime? completedAtUtc,
        byte[] rowVersion)
        : base(id)
    {
        TenantId = tenantId;
        WorkspaceId = workspaceId;
        Kind = kind;
        Status = status;
        EventId = eventId;
        ParametersJson = parametersJson;
        ResultJson = resultJson;
        ErrorCode = errorCode;
        ErrorDescription = errorDescription;
        CreatedAtUtc = createdAtUtc;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        RowVersion = rowVersion;
    }

    private UserInitiatedJob()
        : base(Guid.Empty)
    {
        ParametersJson = "{}";
        RowVersion = Array.Empty<byte>();
    }

    /// <summary>
    /// Obtém o tenant proprietário do job.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Obtém o workspace de escopo do job.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o tipo de trabalho.
    /// </summary>
    public UserInitiatedJobKind Kind { get; private set; }

    /// <summary>
    /// Obtém o status atual.
    /// </summary>
    public UserInitiatedJobStatus Status { get; private set; }

    /// <summary>
    /// Obtém o identificador de evento usado na outbox e no cabeçalho <c>message-id</c>.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Obtém o JSON de parâmetros mínimos do pedido (sem segredos).
    /// </summary>
    public string ParametersJson { get; private set; }

    /// <summary>
    /// Obtém o JSON de resultado quando <see cref="Status"/> é <see cref="UserInitiatedJobStatus.Succeeded"/>.
    /// </summary>
    public string? ResultJson { get; private set; }

    /// <summary>
    /// Obtém o código de erro de negócio quando <see cref="Status"/> é <see cref="UserInitiatedJobStatus.Failed"/>.
    /// </summary>
    public string? ErrorCode { get; private set; }

    /// <summary>
    /// Obtém a descrição do erro quando <see cref="Status"/> é <see cref="UserInitiatedJobStatus.Failed"/>.
    /// </summary>
    public string? ErrorDescription { get; private set; }

    /// <summary>
    /// Obtém o instante UTC de criação do job.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o instante UTC em que o consumidor iniciou a execução.
    /// </summary>
    public DateTime? StartedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o instante UTC de conclusão terminal.
    /// </summary>
    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o token de concorrência otimista.
    /// </summary>
    public byte[] RowVersion { get; private set; }

    /// <summary>
    /// Indica se o job está em estado terminal.
    /// </summary>
    public bool IsTerminal =>
        Status is UserInitiatedJobStatus.Succeeded or UserInitiatedJobStatus.Failed;

    /// <summary>
    /// Cria um job em estado <see cref="UserInitiatedJobStatus.Accepted"/>.
    /// </summary>
    /// <param name="id">Identificador do job.</param>
    /// <param name="tenantId">Tenant contextual.</param>
    /// <param name="workspaceId">Workspace do pedido.</param>
    /// <param name="kind">Tipo de trabalho.</param>
    /// <param name="eventId">EventId alinhado à outbox.</param>
    /// <param name="parametersJson">Parâmetros mínimos serializados.</param>
    /// <param name="createdAtUtc">Instante de criação.</param>
    /// <returns>Job criado ou falha de validação.</returns>
    public static Result<UserInitiatedJob> CreateAccepted(
        Guid id,
        Guid tenantId,
        Guid workspaceId,
        UserInitiatedJobKind kind,
        Guid eventId,
        string parametersJson,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            return Result<UserInitiatedJob>.Failure(UserInitiatedJobErrors.InvalidId);
        }

        if (tenantId == Guid.Empty)
        {
            return Result<UserInitiatedJob>.Failure(UserInitiatedJobErrors.InvalidTenant);
        }

        if (workspaceId == Guid.Empty)
        {
            return Result<UserInitiatedJob>.Failure(UserInitiatedJobErrors.InvalidWorkspace);
        }

        if (eventId == Guid.Empty)
        {
            return Result<UserInitiatedJob>.Failure(UserInitiatedJobErrors.InvalidEventId);
        }

        if (string.IsNullOrWhiteSpace(parametersJson))
        {
            return Result<UserInitiatedJob>.Failure(UserInitiatedJobErrors.InvalidParameters);
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<UserInitiatedJob>.Failure(UserInitiatedJobErrors.InstantNotUtc);
        }

        var job = new UserInitiatedJob(
            id,
            tenantId,
            workspaceId,
            kind,
            UserInitiatedJobStatus.Accepted,
            eventId,
            parametersJson,
            resultJson: null,
            errorCode: null,
            errorDescription: null,
            createdAtUtc,
            startedAtUtc: null,
            completedAtUtc: null,
            rowVersion: Array.Empty<byte>());

        return Result<UserInitiatedJob>.Success(job);
    }

    /// <summary>
    /// Marca o job como em execução no consumidor.
    /// </summary>
    /// <param name="utcNow">Instante UTC da transição.</param>
    /// <returns>Resultado da transição.</returns>
    public Result MarkRunning(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(UserInitiatedJobErrors.InstantNotUtc);
        }

        if (Status is UserInitiatedJobStatus.Running or UserInitiatedJobStatus.Succeeded or UserInitiatedJobStatus.Failed)
        {
            return Status == UserInitiatedJobStatus.Running
                ? Result.Success()
                : Result.Failure(UserInitiatedJobErrors.InvalidTransition);
        }

        if (Status != UserInitiatedJobStatus.Accepted)
        {
            return Result.Failure(UserInitiatedJobErrors.InvalidTransition);
        }

        Status = UserInitiatedJobStatus.Running;
        StartedAtUtc = utcNow;
        return Result.Success();
    }

    /// <summary>
    /// Marca o job como concluído com sucesso.
    /// </summary>
    /// <param name="resultJson">Payload de resultado serializado.</param>
    /// <param name="utcNow">Instante UTC da conclusão.</param>
    /// <returns>Resultado da transição.</returns>
    public Result MarkSucceeded(string? resultJson, DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(UserInitiatedJobErrors.InstantNotUtc);
        }

        if (IsTerminal)
        {
            return Status == UserInitiatedJobStatus.Succeeded
                ? Result.Success()
                : Result.Failure(UserInitiatedJobErrors.InvalidTransition);
        }

        Status = UserInitiatedJobStatus.Succeeded;
        ResultJson = resultJson;
        ErrorCode = null;
        ErrorDescription = null;
        CompletedAtUtc = utcNow;
        return Result.Success();
    }

    /// <summary>
    /// Marca o job como falho com erro de negócio permanente.
    /// </summary>
    /// <param name="error">Erro tipado.</param>
    /// <param name="utcNow">Instante UTC da conclusão.</param>
    /// <returns>Resultado da transição.</returns>
    public Result MarkFailed(Error error, DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            return Result.Failure(UserInitiatedJobErrors.InstantNotUtc);
        }

        if (IsTerminal)
        {
            return Status == UserInitiatedJobStatus.Failed
                ? Result.Success()
                : Result.Failure(UserInitiatedJobErrors.InvalidTransition);
        }

        Status = UserInitiatedJobStatus.Failed;
        ErrorCode = error.Code;
        ErrorDescription = error.Description;
        CompletedAtUtc = utcNow;
        return Result.Success();
    }
}