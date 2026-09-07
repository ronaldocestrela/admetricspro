namespace Tenants.Application.Workspaces.DTOs;

/// <summary>
/// DTO de transferência que representa um cliente/workspace gerenciado pela agência.
/// </summary>
/// <param name="Id">Identificador único do workspace.</param>
/// <param name="Name">Nome comercial do cliente.</param>
/// <param name="CnpjOrCpf">Documento fiscal (CPF/CNPJ).</param>
/// <param name="MonthlyAdSpendBudget">Orçamento mensal estimado de tráfego pago.</param>
/// <param name="Segment">Segmento de mercado.</param>
/// <param name="IsActive">Indica se o workspace está ativo.</param>
/// <param name="CreatedAtUtc">Data e hora de criação em UTC.</param>
/// <param name="UpdatedAtUtc">Data e hora da última modificação em UTC.</param>
public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    string CnpjOrCpf,
    decimal MonthlyAdSpendBudget,
    string? Segment,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
