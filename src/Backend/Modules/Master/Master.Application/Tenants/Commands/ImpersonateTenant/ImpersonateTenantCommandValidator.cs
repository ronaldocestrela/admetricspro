using FluentValidation;

namespace Master.Application.Tenants.Commands.ImpersonateTenant;

/// <summary>
/// Validator for <see cref="ImpersonateTenantCommand"/> enforcing mandatory justification and support ticket reference.
/// </summary>
public sealed class ImpersonateTenantCommandValidator : AbstractValidator<ImpersonateTenantCommand>
{
    /// <summary>
    /// Initializes validation rules for tenant impersonation requests.
    /// </summary>
    public ImpersonateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotNull().WithMessage("TenantId is required.")
            .Must(id => id != null && id.Value != Guid.Empty).WithMessage("TenantId cannot be empty.");

        RuleFor(x => x.SuperAdminId)
            .NotEmpty().WithMessage("SuperAdminId is required and cannot be empty.");

        RuleFor(x => x.SupportTicketId)
            .NotEmpty().WithMessage("Support ticket identifier is required.")
            .MinimumLength(3).WithMessage("Support ticket identifier must have at least 3 characters.")
            .MaximumLength(50).WithMessage("Support ticket identifier cannot exceed 50 characters.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Impersonation reason is mandatory.")
            .MinimumLength(10).WithMessage("Impersonation reason must contain at least 10 characters.")
            .MaximumLength(500).WithMessage("Impersonation reason cannot exceed 500 characters.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(5, 120)
            .WithMessage("Impersonation duration must be between 5 and 120 minutes.");
    }
}
