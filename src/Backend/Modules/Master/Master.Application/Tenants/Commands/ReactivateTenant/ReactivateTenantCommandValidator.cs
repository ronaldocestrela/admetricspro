using FluentValidation;

namespace Master.Application.Tenants.Commands.ReactivateTenant;

/// <summary>
/// Validator for <see cref="ReactivateTenantCommand"/>.
/// </summary>
public sealed class ReactivateTenantCommandValidator : AbstractValidator<ReactivateTenantCommand>
{
    /// <summary>
    /// Initializes validation rules for tenant reactivation.
    /// </summary>
    public ReactivateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotNull().WithMessage("TenantId is required.")
            .Must(id => id != null && id.Value != Guid.Empty).WithMessage("TenantId cannot be empty.");
    }
}
