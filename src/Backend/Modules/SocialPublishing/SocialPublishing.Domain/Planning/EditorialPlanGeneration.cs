using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Planning;

/// <summary>
/// Agregado que registra a materialização idempotente de um plano editorial (chave workspace + intervalo + semente).
/// </summary>
public sealed class EditorialPlanGeneration : AggregateRoot<Guid>
{
    private readonly List<Guid> _createdPostIds = new();
    private readonly List<EditorialPlanFailedSlot> _failedSlots = new();

    private EditorialPlanGeneration(
        Guid id,
        Guid workspaceId,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        int seed,
        EditorialPlanGenerationStatus status,
        DateTime createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        RangeStart = rangeStart;
        RangeEnd = rangeEnd;
        Seed = seed;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    private EditorialPlanGeneration()
        : base(Guid.Empty)
    {
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador do workspace.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém a data inicial inclusiva do plano.
    /// </summary>
    public DateOnly RangeStart { get; private set; }

    /// <summary>
    /// Obtém a data final inclusiva do plano.
    /// </summary>
    public DateOnly RangeEnd { get; private set; }

    /// <summary>
    /// Obtém a semente determinística do plano.
    /// </summary>
    public int Seed { get; private set; }

    /// <summary>
    /// Obtém o status da materialização.
    /// </summary>
    public EditorialPlanGenerationStatus Status { get; private set; }

    /// <summary>
    /// Obtém o carimbo UTC de criação do registro.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém os identificadores dos posts criados com sucesso.
    /// </summary>
    public IReadOnlyList<Guid> CreatedPostIds => _createdPostIds.AsReadOnly();

    /// <summary>
    /// Obtém os slots que falharam na materialização.
    /// </summary>
    public IReadOnlyList<EditorialPlanFailedSlot> FailedSlots => _failedSlots.AsReadOnly();

    /// <summary>
    /// Inicia um novo registro de materialização em andamento.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="rangeStart">Data inicial inclusiva.</param>
    /// <param name="rangeEnd">Data final inclusiva.</param>
    /// <param name="seed">Semente do planner.</param>
    /// <returns>Resultado com o agregado ou erro de validação.</returns>
    public static Result<EditorialPlanGeneration> Start(
        Guid workspaceId,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        int seed)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<EditorialPlanGeneration>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (rangeEnd < rangeStart)
        {
            return Result<EditorialPlanGeneration>.Failure(SocialErrors.PlanRangeInvalid);
        }

        var generation = new EditorialPlanGeneration(
            Guid.NewGuid(),
            workspaceId,
            rangeStart,
            rangeEnd,
            seed,
            EditorialPlanGenerationStatus.InProgress,
            DateTime.UtcNow);

        return Result<EditorialPlanGeneration>.Success(generation);
    }

    /// <summary>
    /// Registra um post criado com sucesso para o plano.
    /// </summary>
    /// <param name="postId">Identificador do post persistido.</param>
    /// <returns>Resultado da operação.</returns>
    public Result RecordCreatedPost(Guid postId)
    {
        if (Status != EditorialPlanGenerationStatus.InProgress)
        {
            return Result.Failure(SocialErrors.PlanGenerationInProgress);
        }

        if (postId == Guid.Empty)
        {
            return Result.Failure(SocialErrors.WorkspaceRequired);
        }

        _createdPostIds.Add(postId);
        return Result.Success();
    }

    /// <summary>
    /// Registra a falha de materialização de um slot.
    /// </summary>
    /// <param name="failedSlot">Detalhes do slot e do erro.</param>
    /// <returns>Resultado da operação.</returns>
    public Result RecordFailedSlot(EditorialPlanFailedSlot failedSlot)
    {
        if (Status != EditorialPlanGenerationStatus.InProgress)
        {
            return Result.Failure(SocialErrors.PlanGenerationInProgress);
        }

        _failedSlots.Add(failedSlot);
        return Result.Success();
    }

    /// <summary>
    /// Marca o plano como concluído, habilitando replay idempotente.
    /// </summary>
    /// <returns>Resultado da operação.</returns>
    public Result Complete()
    {
        if (Status != EditorialPlanGenerationStatus.InProgress)
        {
            return Result.Failure(SocialErrors.PlanGenerationInProgress);
        }

        Status = EditorialPlanGenerationStatus.Completed;
        return Result.Success();
    }
}
