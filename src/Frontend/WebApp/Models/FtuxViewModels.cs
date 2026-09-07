namespace WebApp.Models;

/// <summary>
/// Modelo de formulário para criação rápida de um primeiro cliente/workspace no FTUX.
/// </summary>
/// <param name="Name">Nome comercial do cliente/workspace.</param>
/// <param name="CnpjOrCpf">Documento fiscal (CPF ou CNPJ).</param>
/// <param name="MonthlyAdSpendBudget">Orçamento mensal estimado de investimento em anúncios.</param>
/// <param name="Segment">Nicho ou segmento de mercado.</param>
public sealed record CreateWorkspaceModel(
    string Name,
    string CnpjOrCpf,
    decimal MonthlyAdSpendBudget,
    string? Segment = null);

/// <summary>
/// Modelo para ativação de uma conta de anúncios demonstrativa no FTUX.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="Platform">Plataforma de anúncios (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
public sealed record ConnectDemoAccountModel(
    Guid WorkspaceId,
    string Platform = "MetaAds");

/// <summary>
/// Modelo de formulário para convidar/cadastrar um colaborador na agência.
/// </summary>
/// <param name="FullName">Nome completo do colaborador.</param>
/// <param name="Email">Endereço de e-mail corporativo.</param>
/// <param name="Role">Papel de governança (MediaManager ou Analyst).</param>
/// <param name="PhoneNumber">Telefone opcional.</param>
public sealed record InviteTenantUserModel(
    string FullName,
    string Email,
    string Role,
    string? PhoneNumber = null);

/// <summary>
/// Modelo de formulário para criação rápida de um squad no FTUX.
/// </summary>
/// <param name="Name">Nome do squad/time interno.</param>
/// <param name="Description">Descrição opcional.</param>
public sealed record CreateSquadModel(
    string Name,
    string? Description = null);
