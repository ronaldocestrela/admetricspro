using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Entidade associativa de domínio que representa a alocação de um cliente/workspace (<see cref="Workspace"/>) à carteira de um <see cref="Squad"/>.
/// </summary>
public sealed class SquadWorkspace : Entity<Guid>
{
    private SquadWorkspace(Guid id, Guid squadId, Guid workspaceId, DateTime assignedAtUtc)
        : base(id)
    {
        SquadId = squadId;
        WorkspaceId = workspaceId;
        AssignedAtUtc = assignedAtUtc;
    }

    private SquadWorkspace()
        : base(Guid.Empty)
    {
        SquadId = Guid.Empty;
        WorkspaceId = Guid.Empty;
        AssignedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador único do squad que atende o workspace.
    /// </summary>
    public Guid SquadId { get; private set; }

    /// <summary>
    /// Obtém o identificador único do cliente (<see cref="Workspace"/>) associado.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém a data e hora em UTC da inclusão do cliente na carteira do squad.
    /// </summary>
    public DateTime AssignedAtUtc { get; private set; }

    /// <summary>
    /// Cria uma nova instância válida de <see cref="SquadWorkspace"/>.
    /// </summary>
    /// <param name="squadId">Identificador do squad.</param>
    /// <param name="workspaceId">Identificador do workspace a ser associado.</param>
    /// <param name="assignedAtUtc">Data opcional de alocação.</param>
    /// <returns>Resultado contendo a alocação criada ou erro de validação.</returns>
    public static Result<SquadWorkspace> Create(Guid squadId, Guid workspaceId, DateTime? assignedAtUtc = null)
    {
        if (squadId == Guid.Empty)
        {
            return Result<SquadWorkspace>.Failure(
                Error.Validation("SquadWorkspace.InvalidSquadId", "O identificador do squad não pode ser vazio."));
        }

        if (workspaceId == Guid.Empty)
        {
            return Result<SquadWorkspace>.Failure(
                Error.Validation("SquadWorkspace.InvalidWorkspaceId", "O identificador do workspace não pode ser vazio."));
        }

        return Result<SquadWorkspace>.Success(new SquadWorkspace(
            Guid.NewGuid(),
            squadId,
            workspaceId,
            assignedAtUtc ?? DateTime.UtcNow));
    }
}
