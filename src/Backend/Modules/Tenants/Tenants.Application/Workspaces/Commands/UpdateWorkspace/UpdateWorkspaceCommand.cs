using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Workspaces.Commands.UpdateWorkspace;

/// <summary>
/// Comando para atualizar dados cadastrais e mercadológicos de um workspace existente.
/// </summary>
/// <param name="Id">Identificador único do workspace.</param>
/// <param name="Name">Novo nome comercial do cliente.</param>
/// <param name="CnpjOrCpf">Novo documento fiscal.</param>
/// <param name="MonthlyAdSpendBudget">Novo orçamento mensal de mídia.</param>
/// <param name="Segment">Novo segmento de atuação.</param>
public sealed record UpdateWorkspaceCommand(
    Guid Id,
    string Name,
    string CnpjOrCpf,
    decimal MonthlyAdSpendBudget,
    string? Segment) : ICommand;
