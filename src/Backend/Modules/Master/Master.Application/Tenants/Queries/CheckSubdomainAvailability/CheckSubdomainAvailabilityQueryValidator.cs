using FluentValidation;

namespace Master.Application.Tenants.Queries.CheckSubdomainAvailability;

/// <summary>
/// Validador dos parâmetros de entrada da consulta <see cref="CheckSubdomainAvailabilityQuery"/>.
/// </summary>
public sealed class CheckSubdomainAvailabilityQueryValidator : AbstractValidator<CheckSubdomainAvailabilityQuery>
{
    /// <summary>
    /// Inicializa as regras de validação para verificação de subdomínio.
    /// </summary>
    public CheckSubdomainAvailabilityQueryValidator()
    {
        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("O subdomínio é obrigatório.")
            .MinimumLength(3).WithMessage("O subdomínio deve conter pelo menos 3 caracteres.")
            .MaximumLength(60).WithMessage("O subdomínio não pode exceder 60 caracteres.")
            .Matches(@"^[a-zA-Z0-9-]+$").WithMessage("O subdomínio deve conter apenas letras, números e hífens.");
    }
}
