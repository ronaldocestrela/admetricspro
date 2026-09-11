namespace WebApp.Models;

/// <summary>
/// Modelo de requisição para atualização dos dados cadastrais de um squad.
/// </summary>
/// <param name="Name">Novo nome do time/squad.</param>
/// <param name="Description">Nova descrição opcional.</param>
public sealed record UpdateSquadModel(
    string Name,
    string? Description = null);

/// <summary>
/// Modelo mutável utilizado no formulário de criação e edição de squads no Blazor Server.
/// </summary>
public sealed class SquadFormModel
{
    /// <summary>
    /// Identificador do squad em caso de edição; nulo em caso de novo squad.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Nome de identificação do time/squad.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrição descritiva ou escopo operacional do squad.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Modelo para alocação em lote de membros a um squad.
/// </summary>
public sealed class AssignMultipleMembersModel
{
    /// <summary>
    /// Lista de identificadores de usuários selecionados para vinculação.
    /// </summary>
    public List<Guid> SelectedUserIds { get; set; } = new();
}

/// <summary>
/// Modelo para alocação em lote de clientes/workspaces à carteira de um squad.
/// </summary>
public sealed class AssignMultipleWorkspacesModel
{
    /// <summary>
    /// Lista de identificadores de workspaces selecionados para alocação.
    /// </summary>
    public List<Guid> SelectedWorkspaceIds { get; set; } = new();
}
