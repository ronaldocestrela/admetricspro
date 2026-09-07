using System.ComponentModel.DataAnnotations;
using Master.Domain.Tenants;

namespace WebApp.Models;

/// <summary>
/// Modelo reativo de estado do formulário de onboarding e provisionamento de novo inquilino.
/// </summary>
public sealed class TenantOnboardingFormModel
{
    /// <summary>
    /// Razão Social ou Nome Fantasia da empresa.
    /// </summary>
    [Required(ErrorMessage = "A razão social é obrigatória.")]
    [StringLength(200, ErrorMessage = "O nome não pode exceder 200 caracteres.")]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// CNPJ da empresa (14 dígitos).
    /// </summary>
    [Required(ErrorMessage = "O CNPJ é obrigatório.")]
    [RegularExpression(@"^\d{2}\.\d{3}\.\d{3}\/\d{4}\-\d{2}$|^\d{14}$", ErrorMessage = "CNPJ inválido.")]
    public string Cnpj { get; set; } = string.Empty;

    /// <summary>
    /// Segmento de mercado da organização.
    /// </summary>
    public string Segment { get; set; } = "Agência de Performance";

    /// <summary>
    /// Faixa estimada de investimento mensal em mídia.
    /// </summary>
    public string MonthlyAdSpendRange { get; set; } = "R$ 20.000 a R$ 100.000 / mês";

    /// <summary>
    /// Subdomínio exclusivo de isolamento (ex: 'vanguarda').
    /// </summary>
    [Required(ErrorMessage = "O subdomínio é obrigatório.")]
    [MinLength(3, ErrorMessage = "O subdomínio deve conter pelo menos 3 caracteres.")]
    [MaxLength(60, ErrorMessage = "O subdomínio não pode exceder 60 caracteres.")]
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Domínio personalizado CNAME próprio opcional (ex: 'ads.empresa.com.br').
    /// </summary>
    public string? CustomDomain { get; set; }

    /// <summary>
    /// Cor primária do tema White-Label em formato hexadecimal.
    /// </summary>
    public string PrimaryColor { get; set; } = "#4f46e5";

    /// <summary>
    /// Cor secundária do tema White-Label em formato hexadecimal.
    /// </summary>
    public string SecondaryColor { get; set; } = "#0f172a";

    /// <summary>
    /// Cor de acento do tema White-Label em formato hexadecimal.
    /// </summary>
    public string AccentColor { get; set; } = "#38bdf8";

    /// <summary>
    /// Tier de assinatura selecionado.
    /// </summary>
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Pro;

    /// <summary>
    /// Ciclo de faturamento contratado ('Monthly' ou 'Annual').
    /// </summary>
    public string BillingCycle { get; set; } = "Monthly";

    /// <summary>
    /// Nome completo do administrador / gestor responsável.
    /// </summary>
    [Required(ErrorMessage = "O nome completo do administrador é obrigatório.")]
    public string AdminFullName { get; set; } = string.Empty;

    /// <summary>
    /// E-mail corporativo do administrador.
    /// </summary>
    [Required(ErrorMessage = "O e-mail corporativo é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail com formato inválido.")]
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>
    /// Telefone / WhatsApp comercial de contato.
    /// </summary>
    [Required(ErrorMessage = "O WhatsApp comercial é obrigatório.")]
    public string AdminPhone { get; set; } = string.Empty;

    /// <summary>
    /// Senha de acesso do gestor da conta.
    /// </summary>
    [Required(ErrorMessage = "A senha de acesso é obrigatória.")]
    [MinLength(8, ErrorMessage = "A senha deve conter no mínimo 8 caracteres.")]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmação da senha de acesso.
    /// </summary>
    [Required(ErrorMessage = "A confirmação da senha é obrigatória.")]
    [Compare(nameof(AdminPassword), ErrorMessage = "As senhas não coincidem.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Aceite dos Termos de Uso e Política de Privacidade.
    /// </summary>
    [Range(typeof(bool), "true", "true", ErrorMessage = "É necessário aceitar os termos de serviço.")]
    public bool AcceptTerms { get; set; } = true;

    /// <summary>
    /// Obtém o CNPJ sanitizado (apenas números).
    /// </summary>
    public string GetSanitizedCnpj() => new string(Cnpj?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());

    /// <summary>
    /// Obtém o subdomínio sanitizado (minúsculo e sem espaços).
    /// </summary>
    public string GetSanitizedSubdomain() => (Subdomain ?? string.Empty).Trim().ToLowerInvariant();
}
