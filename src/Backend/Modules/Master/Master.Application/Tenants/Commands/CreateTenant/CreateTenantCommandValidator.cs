using FluentValidation;

namespace Master.Application.Tenants.Commands.CreateTenant;

/// <summary>
/// Validator for incoming <see cref="CreateTenantCommand"/> instances.
/// </summary>
public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    /// <summary>
    /// Initializes validation rules for tenant creation.
    /// </summary>
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200).WithMessage("Company name cannot exceed 200 characters.");

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ is required.")
            .Length(14).WithMessage("CNPJ must contain exactly 14 digits.")
            .Must(cnpj => cnpj.All(char.IsDigit)).WithMessage("CNPJ must contain digits only.");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain is required.")
            .MaximumLength(80).WithMessage("Subdomain cannot exceed 80 characters.")
            .Must(sub => !sub.Any(char.IsWhiteSpace)).WithMessage("Subdomain cannot contain whitespace.");

        RuleFor(x => x.Tier)
            .IsInEnum().WithMessage("Invalid subscription tier.");
    }
}
