using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Posts;

/// <summary>
/// Registro auditável de alteração do horário agendado de um post orgânico.
/// </summary>
public sealed class ScheduleRevision
{
    private ScheduleRevision(
        Guid id,
        DateTime previousScheduledAtUtc,
        DateTime newScheduledAtUtc,
        Guid revisedByUserId,
        DateTime revisedAtUtc)
    {
        Id = id;
        PreviousScheduledAtUtc = previousScheduledAtUtc;
        NewScheduledAtUtc = newScheduledAtUtc;
        RevisedByUserId = revisedByUserId;
        RevisedAtUtc = revisedAtUtc;
    }

    private ScheduleRevision()
    {
    }

    /// <summary>
    /// Obtém o identificador da revisão.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Obtém o horário agendado anterior em UTC.
    /// </summary>
    public DateTime PreviousScheduledAtUtc { get; private set; }

    /// <summary>
    /// Obtém o novo horário agendado em UTC.
    /// </summary>
    public DateTime NewScheduledAtUtc { get; private set; }

    /// <summary>
    /// Obtém o identificador do usuário que realizou a revisão.
    /// </summary>
    public Guid RevisedByUserId { get; private set; }

    /// <summary>
    /// Obtém o carimbo UTC da revisão.
    /// </summary>
    public DateTime RevisedAtUtc { get; private set; }

    /// <summary>
    /// Cria uma revisão de agendamento válida.
    /// </summary>
    /// <param name="previousScheduledAtUtc">Horário anterior em UTC.</param>
    /// <param name="newScheduledAtUtc">Novo horário em UTC.</param>
    /// <param name="revisedByUserId">Usuário autor da alteração.</param>
    /// <param name="revisedAtUtc">Instante da alteração em UTC.</param>
    /// <returns>Resultado com a revisão ou erro de validação.</returns>
    public static Result<ScheduleRevision> Create(
        DateTime previousScheduledAtUtc,
        DateTime newScheduledAtUtc,
        Guid revisedByUserId,
        DateTime revisedAtUtc)
    {
        if (revisedByUserId == Guid.Empty)
        {
            return Result<ScheduleRevision>.Failure(
                Error.Validation("Social.RevisedByRequired", "O identificador do revisor é obrigatório."));
        }

        if (previousScheduledAtUtc.Kind != DateTimeKind.Utc ||
            newScheduledAtUtc.Kind != DateTimeKind.Utc ||
            revisedAtUtc.Kind != DateTimeKind.Utc)
        {
            return Result<ScheduleRevision>.Failure(SocialErrors.ScheduleInstantNotUtc);
        }

        return Result<ScheduleRevision>.Success(
            new ScheduleRevision(
                Guid.NewGuid(),
                previousScheduledAtUtc,
                newScheduledAtUtc,
                revisedByUserId,
                revisedAtUtc));
    }
}
