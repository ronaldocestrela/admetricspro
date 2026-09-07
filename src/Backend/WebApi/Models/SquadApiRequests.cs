namespace WebApi.Models;

/// <summary>
/// Contrato de payload para requisição de criação de squad via Web API.
/// </summary>
/// <param name="Name">Nome de identificação do time/squad.</param>
/// <param name="Description">Descrição ou escopo operacional opcional.</param>
public sealed record CreateSquadApiRequest(string Name, string? Description);

/// <summary>
/// Contrato de payload para atualização cadastral de um squad.
/// </summary>
/// <param name="Name">Novo nome do squad.</param>
/// <param name="Description">Nova descrição ou escopo.</param>
public sealed record UpdateSquadApiRequest(string Name, string? Description);

/// <summary>
/// Contrato de payload para alternar o status operacional do squad.
/// </summary>
/// <param name="IsActive">Status ativo ou inativo.</param>
public sealed record ToggleSquadStatusApiRequest(bool IsActive);

/// <summary>
/// Contrato de payload para inclusão de colaborador como membro do squad.
/// </summary>
/// <param name="UserId">Identificador único do colaborador.</param>
public sealed record AddSquadMemberApiRequest(Guid UserId);

/// <summary>
/// Contrato de payload para alocação de cliente/workspace à carteira do squad.
/// </summary>
/// <param name="WorkspaceId">Identificador único do cliente/workspace.</param>
public sealed record AssignSquadWorkspaceApiRequest(Guid WorkspaceId);
