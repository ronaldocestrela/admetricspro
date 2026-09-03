using System.ComponentModel.DataAnnotations;
using Master.Domain.Tenants;

namespace WebApp.Models;

/// <summary>
/// Modelo de formulário para cadastro e parametrização de planos e tiers de assinatura.
/// </summary>
public sealed class PlanFormViewModel
{
    /// <summary>
    /// Identificador único do plano (nulo no modo de criação).
    /// </summary>
    public Guid? PlanId { get; set; }

    /// <summary>
    /// Nome comercial do plano de assinatura.
    /// </summary>
    [Required(ErrorMessage = "O nome do plano é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrição detalhada do público-alvo e recursos incluídos.
    /// </summary>
    [StringLength(500, ErrorMessage = "A descrição não pode exceder 500 caracteres.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Nível de classificação do tier comercial.
    /// </summary>
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Starter;

    /// <summary>
    /// Preço mensal recorrente em BRL.
    /// </summary>
    [Range(0, 1_000_000, ErrorMessage = "O preço mensal deve ser maior ou igual a zero.")]
    public decimal MonthlyPrice { get; set; } = 99.00m;

    /// <summary>
    /// Percentual de desconto aplicado no ciclo anual (0 a 100%).
    /// </summary>
    [Range(0, 100, ErrorMessage = "O desconto anual deve estar entre 0 e 100%.")]
    public int AnnualDiscountPercentage { get; set; } = 15;

    /// <summary>
    /// Limite máximo de assentos de operadores / gestores.
    /// </summary>
    [Range(1, 10_000, ErrorMessage = "O número de assentos deve ser no mínimo 1.")]
    public int MaxSeats { get; set; } = 5;

    /// <summary>
    /// Limite máximo de workspaces / clientes gerenciados.
    /// </summary>
    [Range(1, 10_000, ErrorMessage = "O número de workspaces deve ser no mínimo 1.")]
    public int MaxWorkspaces { get; set; } = 3;

    /// <summary>
    /// Teto máximo de ad spend gerenciado por mês em BRL.
    /// </summary>
    [Range(0, 100_000_000, ErrorMessage = "O teto de ad spend deve ser maior ou igual a zero.")]
    public decimal MonthlyAdSpendCap { get; set; } = 25_000.00m;

    /// <summary>
    /// Liberação de identidade visual customizada (White-Label completo).
    /// </summary>
    public bool HasWhiteLabel { get; set; }

    /// <summary>
    /// Liberação de domínio personalizado CNAME para o portal do tenant.
    /// </summary>
    public bool HasCustomCname { get; set; }

    /// <summary>
    /// Liberação do copiloto de inteligência artificial para otimização de campanhas.
    /// </summary>
    public bool HasAiCopilot { get; set; }

    /// <summary>
    /// Liberação do motor de automação cross-network e travas de overspending.
    /// </summary>
    public bool HasCrossNetworkAutomations { get; set; } = true;

    /// <summary>
    /// Indica se o formulário está operando em modo de edição ou novo cadastro.
    /// </summary>
    public bool IsEditMode => PlanId.HasValue && PlanId.Value != Guid.Empty;
}
