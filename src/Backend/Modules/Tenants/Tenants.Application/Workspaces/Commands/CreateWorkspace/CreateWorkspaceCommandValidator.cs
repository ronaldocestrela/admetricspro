using BuildingBlocks.Domain.Tenants;
using FluentValidation;

namespace Tenants.Application.Workspaces.Commands.CreateWorkspace;

/// <summary>
/// Validador fluente para o comando <see cref="CreateWorkspaceCommand"/>.
/// </summary>
public sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    /// <summary>
    /// Configura regras de validação para campos de entrada do workspace.
    /// </summary>
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("O nome do workspace é obrigatório.")
            .MaximumLength(150).WithMessage("O nome do workspace não pode exceder 150 caracteres.");

        RuleFor(c => c.CnpjOrCpf)
            .NotEmpty().WithMessage("O documento fiscal é obrigatório.")
            .Must(doc =>
            {
                var sanitized = TaxDocumentValidator.Sanitize(doc);
                return TaxDocumentValidator.IsValidFormat(sanitized) &&
                       (TaxDocumentValidator.IsValidCpf(sanitized) || TaxDocumentValidator.IsValidCnpj(sanitized));
            }).WithMessage("O documento informado deve ser um CPF ou CNPJ válido.");

        RuleFor(c => c.MonthlyAdSpendBudget)
            .GreaterThanOrEqualTo(0).WithMessage("O orçamento mensal de mídia não pode ser negativo.");

        RuleFor(c => c.Segment)
            .MaximumLength(100).WithMessage("O segmento não pode exceder 100 caracteres.")
            .When(c => !string.IsNullOrWhiteSpace(c.Segment));
    }
}
