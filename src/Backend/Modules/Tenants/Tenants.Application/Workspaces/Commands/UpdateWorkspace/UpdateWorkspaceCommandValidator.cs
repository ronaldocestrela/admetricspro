using BuildingBlocks.Domain.Tenants;
using FluentValidation;

namespace Tenants.Application.Workspaces.Commands.UpdateWorkspace;

/// <summary>
/// Validador fluente para o comando <see cref="UpdateWorkspaceCommand"/>.
/// </summary>
public sealed class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    /// <summary>
    /// Configura regras de validação para a atualização de workspace.
    /// </summary>
    public UpdateWorkspaceCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty().WithMessage("O identificador do workspace é obrigatório.");

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
