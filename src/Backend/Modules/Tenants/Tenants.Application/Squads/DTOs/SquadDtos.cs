namespace Tenants.Application.Squads.DTOs;

/// <summary>
/// DTO sumarizado para listagens gerais de squads da agência.
/// </summary>
/// <param name="Id">Identificador único do squad.</param>
/// <param name="Name">Nome do squad.</param>
/// <param name="Description">Descrição ou escopo operacional do squad.</param>
/// <param name="IsActive">Status operacional do squad.</param>
/// <param name="MemberCount">Quantidade total de membros vinculados.</param>
/// <param name="WorkspaceCount">Quantidade total de clientes/workspaces alocados na carteira.</param>
/// <param name="CreatedAtUtc">Data e hora em UTC de criação.</param>
public sealed record SquadSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int MemberCount,
    int WorkspaceCount,
    DateTime CreatedAtUtc);

/// <summary>
/// DTO detalhado de um membro associado ao squad.
/// </summary>
/// <param name="UserId">Identificador único do usuário.</param>
/// <param name="FullName">Nome completo do colaborador.</param>
/// <param name="Email">E-mail corporativo.</param>
/// <param name="Role">Papel funcional do usuário no inquilino.</param>
/// <param name="JoinedAtUtc">Data e hora em UTC de ingresso no squad.</param>
public sealed record SquadMemberDto(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    DateTime JoinedAtUtc);

/// <summary>
/// DTO detalhado de um cliente/workspace alocado na carteira do squad.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace.</param>
/// <param name="Name">Nome comercial ou razão social do cliente.</param>
/// <param name="CnpjOrCpf">Documento fiscal do cliente.</param>
/// <param name="MonthlyAdSpendBudget">Orçamento mensal de mídia.</param>
/// <param name="Segment">Segmento de atuação.</param>
/// <param name="AssignedAtUtc">Data e hora em UTC da alocação à carteira do squad.</param>
public sealed record SquadWorkspaceDto(
    Guid WorkspaceId,
    string Name,
    string CnpjOrCpf,
    decimal MonthlyAdSpendBudget,
    string? Segment,
    DateTime AssignedAtUtc);

/// <summary>
/// DTO completo contendo os detalhes do squad, seus membros e sua carteira de workspaces.
/// </summary>
/// <param name="Id">Identificador único do squad.</param>
/// <param name="Name">Nome do squad.</param>
/// <param name="Description">Descrição ou notas de escopo.</param>
/// <param name="IsActive">Status operacional.</param>
/// <param name="Members">Lista de membros vinculados ao squad.</param>
/// <param name="Workspaces">Lista de clientes/workspaces alocados na carteira.</param>
/// <param name="CreatedAtUtc">Data e hora de criação em UTC.</param>
/// <param name="UpdatedAtUtc">Data e hora da última modificação em UTC.</param>
public sealed record SquadDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<SquadMemberDto> Members,
    IReadOnlyList<SquadWorkspaceDto> Workspaces,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
