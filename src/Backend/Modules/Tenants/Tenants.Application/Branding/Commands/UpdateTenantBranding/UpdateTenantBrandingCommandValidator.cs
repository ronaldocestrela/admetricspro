using FluentValidation;

namespace Tenants.Application.Branding.Commands.UpdateTenantBranding;

/// <summary>
/// Validador fluente para o comando <see cref="UpdateTenantBrandingCommand"/>.
/// </summary>
public sealed class UpdateTenantBrandingCommandValidator : AbstractValidator<UpdateTenantBrandingCommand>
{
    /// <summary>
    /// Configura regras de validação para os atributos visuais de White-Label.
    /// </summary>
    public UpdateTenantBrandingCommandValidator()
    {
        RuleFor(c => c.PrimaryColor)
            .NotEmpty().WithMessage("A cor primária é obrigatória.")
            .MaximumLength(9).WithMessage("A cor primária não pode exceder 9 caracteres.");

        RuleFor(c => c.SecondaryColor)
            .NotEmpty().WithMessage("A cor secundária é obrigatória.")
            .MaximumLength(9).WithMessage("A cor secundária não pode exceder 9 caracteres.");

        RuleFor(c => c.LightLogoUrl)
            .MaximumLength(1000).WithMessage("A URL do logo claro não pode exceder 1000 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.LightLogoUrl));

        RuleFor(c => c.DarkLogoUrl)
            .MaximumLength(1000).WithMessage("A URL do logo escuro não pode exceder 1000 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.DarkLogoUrl));

        RuleFor(c => c.FaviconUrl)
            .MaximumLength(1000).WithMessage("A URL do favicon não pode exceder 1000 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.FaviconUrl));
    }
}
