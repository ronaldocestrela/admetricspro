using FluentValidation;

namespace Master.Application.Tenants.Commands.SuspendTenant;

/// <summary>
/// Validator for <see cref="SuspendTenantCommand"/>.
/// </summary>
public sealed class SuspendTenantCommandValidator : AbstractValidator<SuspendTenantCommand>
{
    /// <summary>
    /// Initializes validation rules for tenant suspension.
    /// </summary>
    public SuspendTenantCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotNull().WithMessage("TenantId is required.")
            .Must(id => id != null && id.Value != Guid.Empty).WithMessage("TenantId cannot be empty.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Suspension reason is required.")
            .MaximumLength(500).WithMessage("Suspension reason cannot exceed 500 characters.");
    }
}
