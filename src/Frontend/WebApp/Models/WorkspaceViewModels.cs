namespace WebApp.Models;

/// <summary>
/// Modelo de requisição para atualização dos dados cadastrais de um workspace existente.
/// </summary>
/// <param name="Name">Novo nome comercial do cliente/workspace.</param>
/// <param name="CnpjOrCpf">Novo documento fiscal (CPF ou CNPJ).</param>
/// <param name="MonthlyAdSpendBudget">Novo orçamento mensal de mídia.</param>
/// <param name="Segment">Novo segmento ou nicho de mercado.</param>
public sealed record UpdateWorkspaceModel(
    string Name,
    string CnpjOrCpf,
    decimal MonthlyAdSpendBudget,
    string? Segment = null);

/// <summary>
/// Modelo mutável utilizado no formulário interativo de criação e edição de Workspaces no Blazor Server.
/// </summary>
public sealed class WorkspaceFormModel
{
    /// <summary>
    /// Identificador do workspace em caso de edição; nulo em caso de criação.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Razão social ou nome comercial do cliente.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// CNPJ ou CPF do cliente.
    /// </summary>
    public string CnpjOrCpf { get; set; } = string.Empty;

    /// <summary>
    /// Orçamento mensal previsto para campanhas de mídia paga.
    /// </summary>
    public decimal MonthlyAdSpendBudget { get; set; } = 5000m;

    /// <summary>
    /// Segmento ou nicho mercadológico do cliente.
    /// </summary>
    public string? Segment { get; set; }
}
