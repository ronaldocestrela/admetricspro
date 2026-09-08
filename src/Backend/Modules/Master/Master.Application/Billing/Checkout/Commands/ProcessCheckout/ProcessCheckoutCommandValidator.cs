using FluentValidation;
using Master.Domain.Billing;
using Master.Domain.Tenants;

namespace Master.Application.Billing.Checkout.Commands.ProcessCheckout;

/// <summary>
/// Validador dos dados de entrada para o comando de checkout de assinatura (<see cref="ProcessCheckoutCommand"/>).
/// </summary>
public sealed class ProcessCheckoutCommandValidator : AbstractValidator<ProcessCheckoutCommand>
{
    /// <summary>
    /// Inicializa as regras de validação do comando de checkout.
    /// </summary>
    public ProcessCheckoutCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("O identificador do tenant é obrigatório.");

        RuleFor(x => x.Tier)
            .IsInEnum().WithMessage("Classificação de tier inválida.")
            .NotEqual(SubscriptionTier.Trial).WithMessage("Não é permitido realizar checkout para o plano Trial.");

        RuleFor(x => x.BillingCycle)
            .NotEmpty().WithMessage("O ciclo de faturamento é obrigatório.")
            .Must(cycle => string.Equals(cycle, "Monthly", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(cycle, "Annual", StringComparison.OrdinalIgnoreCase))
            .WithMessage("O ciclo de faturamento deve ser 'Monthly' ou 'Annual'.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Método de pagamento inválido.");

        When(x => x.PaymentMethod == PaymentMethod.CreditCard, () =>
        {
            RuleFor(x => x.CardHolderName)
                .NotEmpty().WithMessage("O nome impresso no cartão é obrigatório.")
                .MaximumLength(100).WithMessage("O nome impresso não pode exceder 100 caracteres.");

            RuleFor(x => x.CardNumber)
                .NotEmpty().WithMessage("O número do cartão de crédito é obrigatório.")
                .Matches(@"^\d{13,19}$").WithMessage("O número do cartão deve conter entre 13 e 19 dígitos numéricos.");

            RuleFor(x => x.ExpiryMonth)
                .NotEmpty().WithMessage("O mês de expiração é obrigatório.")
                .Matches(@"^(0[1-9]|1[0-2])$").WithMessage("O mês de expiração deve estar no formato 01 a 12.");

            RuleFor(x => x.ExpiryYear)
                .NotEmpty().WithMessage("O ano de expiração é obrigatório.")
                .Matches(@"^\d{4}$").WithMessage("O ano de expiração deve conter 4 dígitos.");

            RuleFor(x => x.Ccv)
                .NotEmpty().WithMessage("O código de segurança (CVV/CCV) é obrigatório.")
                .Matches(@"^\d{3,4}$").WithMessage("O código de segurança deve conter 3 ou 4 dígitos.");
        });
    }
}
