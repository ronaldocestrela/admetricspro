using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Entidade associativa de domínio que representa a alocação de um colaborador (<see cref="TenantUser"/>) em um <see cref="Squad"/>.
/// </summary>
public sealed class SquadMember : Entity<Guid>
{
    private SquadMember(Guid id, Guid squadId, Guid userId, DateTime joinedAtUtc)
        : base(id)
    {
        SquadId = squadId;
        UserId = userId;
        JoinedAtUtc = joinedAtUtc;
    }

    private SquadMember()
        : base(Guid.Empty)
    {
        SquadId = Guid.Empty;
        UserId = Guid.Empty;
        JoinedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador único do squad ao qual o membro está vinculado.
    /// </summary>
    public Guid SquadId { get; private set; }

    /// <summary>
    /// Obtém o identificador único do usuário do inquilino (<see cref="TenantUser"/>) alocado no squad.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Obtém a data e hora em UTC de ingresso do colaborador no squad.
    /// </summary>
    public DateTime JoinedAtUtc { get; private set; }

    /// <summary>
    /// Cria uma nova instância válida de <see cref="SquadMember"/>.
    /// </summary>
    /// <param name="squadId">Identificador do squad.</param>
    /// <param name="userId">Identificador do usuário a ser vinculado.</param>
    /// <param name="joinedAtUtc">Data opcional de ingresso.</param>
    /// <returns>Resultado contendo o vínculo criado ou erro de validação.</returns>
    public static Result<SquadMember> Create(Guid squadId, Guid userId, DateTime? joinedAtUtc = null)
    {
        if (squadId == Guid.Empty)
        {
            return Result<SquadMember>.Failure(
                Error.Validation("SquadMember.InvalidSquadId", "O identificador do squad não pode ser vazio."));
        }

        if (userId == Guid.Empty)
        {
            return Result<SquadMember>.Failure(
                Error.Validation("SquadMember.InvalidUserId", "O identificador do usuário não pode ser vazio."));
        }

        return Result<SquadMember>.Success(new SquadMember(
            Guid.NewGuid(),
            squadId,
            userId,
            joinedAtUtc ?? DateTime.UtcNow));
    }
}
