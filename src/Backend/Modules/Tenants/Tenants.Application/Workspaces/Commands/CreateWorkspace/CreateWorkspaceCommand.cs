using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Workspaces.Commands.CreateWorkspace;

/// <summary>
/// Comando de criação de um novo cliente/workspace no banco dedicado do inquilino.
/// </summary>
/// <param name="Name">Nome comercial do cliente/workspace.</param>
/// <param name="CnpjOrCpf">Documento fiscal (CPF ou CNPJ).</param>
/// <param name="MonthlyAdSpendBudget">Orçamento mensal estimado de investimento em anúncios.</param>
/// <param name="Segment">Segmento opcional de atuação.</param>
public sealed record CreateWorkspaceCommand(
    string Name,
    string CnpjOrCpf,
    decimal MonthlyAdSpendBudget,
    string? Segment) : ICommand<Guid>;
