using FluentValidation;

namespace Master.Application.Tenants.Queries.CheckTaxDocumentAvailability;

/// <summary>
/// Validador dos parâmetros de entrada da consulta <see cref="CheckTaxDocumentAvailabilityQuery"/>.
/// </summary>
public sealed class CheckTaxDocumentAvailabilityQueryValidator : AbstractValidator<CheckTaxDocumentAvailabilityQuery>
{
    /// <summary>
    /// Inicializa as regras de validação básica da consulta de documento.
    /// </summary>
    public CheckTaxDocumentAvailabilityQueryValidator()
    {
        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("O documento fiscal deve ser informado.")
            .MaximumLength(30).WithMessage("O documento fiscal não pode exceder 30 caracteres.");
    }
}
