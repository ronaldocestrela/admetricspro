using FluentValidation;

namespace Master.Application.Tenants.Commands.RegisterTenantOnboarding;

/// <summary>
/// Validador dos dados de submissão do onboarding de tenant (<see cref="RegisterTenantOnboardingCommand"/>).
/// </summary>
public sealed class RegisterTenantOnboardingCommandValidator : AbstractValidator<RegisterTenantOnboardingCommand>
{
    private static readonly HashSet<string> ReservedSubdomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "api", "admin", "master", "app", "auth", "status", "backoffice", "portal",
        "system", "dashboard", "billing", "login", "register", "onboarding", "scalar", "swagger"
    };

    /// <summary>
    /// Inicializa regras estritas de validação para novos inquilinos.
    /// </summary>
    public RegisterTenantOnboardingCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Razão social ou nome da empresa é obrigatório.")
            .MaximumLength(200).WithMessage("Nome da empresa não pode exceder 200 caracteres.");

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CPF ou CNPJ é obrigatório.")
            .Must(doc => doc != null && (doc.Length == 11 || doc.Length == 14) && doc.All(char.IsDigit))
            .WithMessage("O documento fiscal deve conter exatamente 11 dígitos (CPF) ou 14 dígitos (CNPJ) numéricos.");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomínio é obrigatório.")
            .MinimumLength(3).WithMessage("Subdomínio deve conter no mínimo 3 caracteres.")
            .MaximumLength(60).WithMessage("Subdomínio não pode exceder 60 caracteres.")
            .Must(sub => !string.IsNullOrWhiteSpace(sub) && !sub.Any(char.IsWhiteSpace))
            .WithMessage("Subdomínio não pode conter espaços.")
            .Matches(@"^[a-zA-Z0-9-]+$").WithMessage("Subdomínio deve conter apenas letras, números e hífens.")
            .Must(sub => !ReservedSubdomains.Contains(sub.Trim().ToLowerInvariant()))
            .WithMessage("O subdomínio informado é reservado pelo sistema.");

        RuleFor(x => x.Tier)
            .IsInEnum().WithMessage("Tier de assinatura inválido.");

        RuleFor(x => x.AdminFullName)
            .NotEmpty().WithMessage("Nome completo do administrador é obrigatório.")
            .MaximumLength(150).WithMessage("Nome não pode exceder 150 caracteres.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("E-mail do administrador é obrigatório.")
            .EmailAddress().WithMessage("E-mail com formato inválido.")
            .MaximumLength(200).WithMessage("E-mail não pode exceder 200 caracteres.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Senha de acesso é obrigatória.")
            .MinimumLength(8).WithMessage("A senha deve conter no mínimo 8 caracteres.");
    }
}
