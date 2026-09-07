using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Agregado de domínio raiz que representa um Squad (time interno ou célula de operação) da agência de tráfego.
/// </summary>
public sealed class Squad : AggregateRoot<Guid>
{
    private readonly List<SquadMember> _members = new();
    private readonly List<SquadWorkspace> _workspaces = new();

    private Squad(
        Guid id,
        string name,
        string? description,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
        : base(id)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private Squad()
        : base(Guid.Empty)
    {
        Name = string.Empty;
        Description = null;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = null;
    }

    /// <summary>
    /// Obtém o nome de identificação do time/squad (ex: "Squad E-commerce", "Célula Branding").
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Obtém a descrição opcional ou escopo funcional do squad.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Obtém o status operacional do squad (ativo ou pausado).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Obtém a data e hora em UTC de criação do squad.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém a data e hora em UTC da última modificação cadastral, se houver.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém a coleção de colaboradores vinculados a este squad.
    /// </summary>
    public IReadOnlyCollection<SquadMember> Members => _members.AsReadOnly();

    /// <summary>
    /// Obtém a coleção de clientes/workspaces alocados na carteira deste squad.
    /// </summary>
    public IReadOnlyCollection<SquadWorkspace> Workspaces => _workspaces.AsReadOnly();

    /// <summary>
    /// Cria uma nova instância de <see cref="Squad"/> com validação de invariantes de domínio.
    /// </summary>
    /// <param name="id">Identificador único do squad.</param>
    /// <param name="name">Nome do squad.</param>
    /// <param name="description">Descrição ou observações operacionais.</param>
    /// <returns>Resultado contendo a nova entidade ou erro de validação.</returns>
    public static Result<Squad> Create(Guid id, string name, string? description)
    {
        if (id == Guid.Empty)
        {
            id = Guid.NewGuid();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Squad>.Failure(
                Error.Validation("Squad.EmptyName", "O nome do squad é obrigatório e não pode ser vazio."));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 100)
        {
            return Result<Squad>.Failure(
                Error.Validation("Squad.NameTooLong", "O nome do squad não pode ultrapassar 100 caracteres."));
        }

        string? trimmedDesc = null;
        if (!string.IsNullOrWhiteSpace(description))
        {
            trimmedDesc = description.Trim();
            if (trimmedDesc.Length > 500)
            {
                return Result<Squad>.Failure(
                    Error.Validation("Squad.DescriptionTooLong", "A descrição do squad não pode ultrapassar 500 caracteres."));
            }
        }

        var squad = new Squad(
            id,
            trimmedName,
            trimmedDesc,
            isActive: true,
            createdAtUtc: DateTime.UtcNow,
            updatedAtUtc: null);

        return Result<Squad>.Success(squad);
    }

    /// <summary>
    /// Atualiza os dados descritivos e cadastrais do squad.
    /// </summary>
    /// <param name="name">Novo nome do squad.</param>
    /// <param name="description">Nova descrição opcional.</param>
    /// <returns>Resultado de sucesso ou erro de validação.</returns>
    public Result UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                Error.Validation("Squad.EmptyName", "O nome do squad é obrigatório e não pode ser vazio."));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 100)
        {
            return Result.Failure(
                Error.Validation("Squad.NameTooLong", "O nome do squad não pode ultrapassar 100 caracteres."));
        }

        string? trimmedDesc = null;
        if (!string.IsNullOrWhiteSpace(description))
        {
            trimmedDesc = description.Trim();
            if (trimmedDesc.Length > 500)
            {
                return Result.Failure(
                    Error.Validation("Squad.DescriptionTooLong", "A descrição do squad não pode ultrapassar 500 caracteres."));
            }
        }

        Name = trimmedName;
        Description = trimmedDesc;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Altera o status operacional do squad (ativo ou inativo).
    /// </summary>
    /// <param name="isActive">Novo status do squad.</param>
    public void ToggleStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Adiciona um colaborador à equipe do squad.
    /// </summary>
    /// <param name="userId">Identificador do usuário do inquilino.</param>
    /// <returns>Resultado da operação com erro de conflito caso o colaborador já pertença ao time.</returns>
    public Result AddMember(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(
                Error.Validation("Squad.InvalidUserId", "O identificador do usuário não pode ser vazio."));
        }

        if (_members.Any(m => m.UserId == userId))
        {
            return Result.Failure(
                Error.Conflict("Squad.MemberAlreadyExists", "Este colaborador já está vinculado a este squad."));
        }

        var memberResult = SquadMember.Create(Id, userId);
        if (memberResult.IsFailure)
        {
            return Result.Failure(memberResult.Error);
        }

        _members.Add(memberResult.Value);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Remove um colaborador da equipe do squad.
    /// </summary>
    /// <param name="userId">Identificador do usuário a ser desvinculado.</param>
    /// <returns>Resultado da operação com erro caso o colaborador não seja membro do squad.</returns>
    public Result RemoveMember(Guid userId)
    {
        var existing = _members.FirstOrDefault(m => m.UserId == userId);
        if (existing is null)
        {
            return Result.Failure(
                Error.NotFound("Squad.MemberNotFound", "O colaborador especificado não é membro deste squad."));
        }

        _members.Remove(existing);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Aloca um cliente/workspace à carteira operacional deste squad.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace a ser associado.</param>
    /// <returns>Resultado da operação com erro de conflito caso o cliente já esteja na carteira.</returns>
    public Result AssignWorkspace(Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result.Failure(
                Error.Validation("Squad.InvalidWorkspaceId", "O identificador do workspace não pode ser vazio."));
        }

        if (_workspaces.Any(w => w.WorkspaceId == workspaceId))
        {
            return Result.Failure(
                Error.Conflict("Squad.WorkspaceAlreadyAssigned", "Este cliente/workspace já está alocado a este squad."));
        }

        var workspaceResult = SquadWorkspace.Create(Id, workspaceId);
        if (workspaceResult.IsFailure)
        {
            return Result.Failure(workspaceResult.Error);
        }

        _workspaces.Add(workspaceResult.Value);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Remove um cliente/workspace da carteira operacional deste squad.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace a ser desvinculado.</param>
    /// <returns>Resultado da operação com erro caso o cliente não pertença à carteira do squad.</returns>
    public Result UnassignWorkspace(Guid workspaceId)
    {
        var existing = _workspaces.FirstOrDefault(w => w.WorkspaceId == workspaceId);
        if (existing is null)
        {
            return Result.Failure(
                Error.NotFound("Squad.WorkspaceNotAssigned", "Este cliente/workspace não está alocado a este squad."));
        }

        _workspaces.Remove(existing);
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }
}
