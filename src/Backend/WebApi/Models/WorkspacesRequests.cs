namespace WebApi.Models;

/// <summary>
/// Carga útil para criação de um novo cliente/workspace da agência.
/// </summary>
/// <param name="Name">Nome comercial do cliente/workspace.</param>
/// <param name="CnpjOrCpf">Documento fiscal (CPF ou CNPJ) com ou sem máscara.</param>
/// <param name="MonthlyAdSpendBudget">Orçamento mensal estimado de investimento em anúncios.</param>
/// <param name="Segment">Segmento opcional de mercado (ex: E-commerce, Infoproduto, Local).</param>
public sealed record CreateWorkspaceApiRequest(
    string Name,
    string CnpjOrCpf,
    decimal MonthlyAdSpendBudget,
    string? Segment);

/// <summary>
/// Carga útil para atualização dos dados cadastrais de um workspace existente.
/// </summary>
/// <param name="Name">Novo nome comercial do cliente/workspace.</param>
/// <param name="CnpjOrCpf">Novo documento fiscal.</param>
/// <param name="MonthlyAdSpendBudget">Novo orçamento mensal estimado.</param>
/// <param name="Segment">Novo segmento de atuação.</param>
public sealed record UpdateWorkspaceApiRequest(
    string Name,
    string CnpjOrCpf,
    decimal MonthlyAdSpendBudget,
    string? Segment);
