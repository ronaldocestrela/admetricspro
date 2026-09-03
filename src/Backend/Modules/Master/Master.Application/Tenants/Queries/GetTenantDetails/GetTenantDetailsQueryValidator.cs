using FluentValidation;

namespace Master.Application.Tenants.Queries.GetTenantDetails;

/// <summary>
/// Validator for <see cref="GetTenantDetailsQuery"/>.
/// </summary>
public sealed class GetTenantDetailsQueryValidator : AbstractValidator<GetTenantDetailsQuery>
{
    /// <summary>
    /// Initializes validation rules for tenant details query.
    /// </summary>
    public GetTenantDetailsQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotNull().WithMessage("TenantId is required.")
            .Must(id => id != null && id.Value != Guid.Empty).WithMessage("TenantId cannot be empty.");
    }
}
