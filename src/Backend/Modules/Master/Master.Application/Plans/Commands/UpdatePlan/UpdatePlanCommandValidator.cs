using FluentValidation;

namespace Master.Application.Plans.Commands.UpdatePlan;

/// <summary>
/// Validator for <see cref="UpdatePlanCommand"/> inputs.
/// </summary>
public sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    /// <summary>
    /// Initializes validation rules for plan updates.
    /// </summary>
    public UpdatePlanCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("O identificador do plano é obrigatório.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do plano é obrigatório.")
            .MaximumLength(100).WithMessage("O nome do plano não pode exceder 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A descrição do plano não pode exceder 500 caracteres.");

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
