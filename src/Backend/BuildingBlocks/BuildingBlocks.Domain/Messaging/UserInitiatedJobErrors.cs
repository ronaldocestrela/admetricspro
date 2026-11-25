using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Messaging;

/// <summary>
/// Erros de validação e transição de <see cref="UserInitiatedJob"/>.
/// </summary>
public static class UserInitiatedJobErrors
{
    /// <summary>
    /// Identificador do job inválido.
    /// </summary>
    public static readonly Error InvalidId = Error.Validation(
        "UserJob.InvalidId",
        "O identificador do job é obrigatório.");

    /// <summary>
    /// Workspace inválido.
    /// </summary>
    public static readonly Error InvalidWorkspace = Error.Validation(
        "UserJob.InvalidWorkspace",
        "O workspace é obrigatório.");

    /// <summary>
    /// Tenant inválido.
    /// </summary>
    public static readonly Error InvalidTenant = Error.Validation(
        "UserJob.InvalidTenant",
        "O tenant é obrigatório.");

    /// <summary>
    /// EventId inválido.
    /// </summary>
    public static readonly Error InvalidEventId = Error.Validation(
        "UserJob.InvalidEventId",
        "O EventId do job é obrigatório.");

    /// <summary>
    /// Parâmetros JSON ausentes.
    /// </summary>
    public static readonly Error InvalidParameters = Error.Validation(
        "UserJob.InvalidParameters",
        "Os parâmetros do job são obrigatórios.");

    /// <summary>
    /// Instantâneo fora de UTC.
    /// </summary>
    public static readonly Error InstantNotUtc = Error.Validation(
        "UserJob.InstantNotUtc",
        "Carimbos de tempo devem estar em UTC.");

    /// <summary>
    /// Transição de estado inválida.
    /// </summary>
    public static readonly Error InvalidTransition = Error.Validation(
        "UserJob.InvalidTransition",
        "Transição de status do job não permitida.");

    /// <summary>
    /// Job não encontrado.
    /// </summary>
    public static readonly Error NotFound = Error.NotFound(
        "UserJob.NotFound",
        "Job não localizado para este workspace.");
}