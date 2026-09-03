using FluentValidation;

namespace Master.Application.Plans.Commands.CreatePlan;

/// <summary>
/// Validator for <see cref="CreatePlanCommand"/> inputs.
/// </summary>
public sealed class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    /// <summary>
    /// Initializes validation rules for plan creation.
    /// </summary>
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do plano é obrigatório.")
            .MaximumLength(100).WithMessage("O nome do plano não pode exceder 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A descrição do plano não pode exceder 500 caracteres.");

        RuleFor(x => x.Tier)
            .IsInEnum().WithMessage("Classificação de tier inválida.");

        RuleFor(x => x.MonthlyPrice)
            .GreaterThanOrEqualTo(0).WithMessage("O preço mensal não pode ser negativo.");

        RuleFor(x => x.AnnualDiscountPercentage)
            .InclusiveBetween(0, 100).WithMessage("O desconto anual deve estar entre 0 e 100%.");

        RuleFor(x => x.MaxSeats)
            .GreaterThan(0).WithMessage("O limite de assentos deve ser maior que zero.");

        RuleFor(x => x.MaxWorkspaces)
            .GreaterThan(0).WithMessage("O limite de workspaces deve ser maior que zero.");

        RuleFor(x => x.MonthlyAdSpendCap)
            .GreaterThanOrEqualTo(0).WithMessage("O teto de ad spend mensal não pode ser negativo.");
    }
}
