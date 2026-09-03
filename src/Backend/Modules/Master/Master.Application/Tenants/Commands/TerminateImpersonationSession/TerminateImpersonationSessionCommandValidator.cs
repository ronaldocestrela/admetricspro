using FluentValidation;

namespace Master.Application.Tenants.Commands.TerminateImpersonationSession;

/// <summary>
/// Validador de contrato para o comando <see cref="TerminateImpersonationSessionCommand"/>.
/// </summary>
public sealed class TerminateImpersonationSessionCommandValidator : AbstractValidator<TerminateImpersonationSessionCommand>
{
    /// <summary>
    /// Define as regras de validação para encerramento de sessão de impersonation.
    /// </summary>
    public TerminateImpersonationSessionCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant identifier is required.");

        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session identifier is required.");
    }
}
