using Master.Domain.Tenants;

namespace WebApi.Models;

/// <summary>
/// Requisição para criação de um novo plano de assinatura.
/// </summary>
/// <param name="Name">Nome comercial do plano.</param>
/// <param name="Description">Descrição detalhada do plano.</param>
/// <param name="Tier">Nível do tier contratual.</param>
/// <param name="MonthlyPrice">Preço mensal em BRL.</param>
/// <param name="AnnualDiscountPercentage">Percentual de desconto anual (0 a 100).</param>
/// <param name="MaxSeats">Limite máximo de assentos de usuários.</param>
/// <param name="MaxWorkspaces">Limite máximo de workspaces gerenciados.</param>
/// <param name="MonthlyAdSpendCap">Teto mensal de ad spend gerenciado.</param>
/// <param name="HasWhiteLabel">Liberação de white-label integral.</param>
/// <param name="HasCustomCname">Liberação de domínio personalizado CNAME.</param>
/// <param name="HasAiCopilot">Liberação do copiloto de IA.</param>
/// <param name="HasCrossNetworkAutomations">Liberação do motor de automação cross-network.</param>
public sealed record CreatePlanApiRequest(
    string Name,
    string Description,
    SubscriptionTier Tier,
    decimal MonthlyPrice,
    int AnnualDiscountPercentage,
    int MaxSeats,
    int MaxWorkspaces,
    decimal MonthlyAdSpendCap,
    bool HasWhiteLabel,
    bool HasCustomCname,
    bool HasAiCopilot,
    bool HasCrossNetworkAutomations);

/// <summary>
/// Requisição para atualização de um plano de assinatura existente.
/// </summary>
/// <param name="Name">Nome comercial do plano.</param>
/// <param name="Description">Descrição detalhada do plano.</param>
/// <param name="MonthlyPrice">Preço mensal em BRL.</param>
/// <param name="AnnualDiscountPercentage">Percentual de desconto anual (0 a 100).</param>
/// <param name="MaxSeats">Limite máximo de assentos de usuários.</param>
/// <param name="MaxWorkspaces">Limite máximo de workspaces gerenciados.</param>
/// <param name="MonthlyAdSpendCap">Teto mensal de ad spend gerenciado.</param>
/// <param name="HasWhiteLabel">Liberação de white-label integral.</param>
/// <param name="HasCustomCname">Liberação de domínio personalizado CNAME.</param>
/// <param name="HasAiCopilot">Liberação do copiloto de IA.</param>
/// <param name="HasCrossNetworkAutomations">Liberação do motor de automação cross-network.</param>
public sealed record UpdatePlanApiRequest(
    string Name,
    string Description,
    decimal MonthlyPrice,
    int AnnualDiscountPercentage,
    int MaxSeats,
    int MaxWorkspaces,
    decimal MonthlyAdSpendCap,
    bool HasWhiteLabel,
    bool HasCustomCname,
    bool HasAiCopilot,
    bool HasCrossNetworkAutomations);
