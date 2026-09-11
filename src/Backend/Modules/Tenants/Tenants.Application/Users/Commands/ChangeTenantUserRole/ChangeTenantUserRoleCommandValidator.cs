using FluentValidation;

namespace Tenants.Application.Users.Commands.ChangeTenantUserRole;

/// <summary>
/// Validador das regras de integridade do comando <see cref="ChangeTenantUserRoleCommand"/>.
/// </summary>
public sealed class ChangeTenantUserRoleCommandValidator : AbstractValidator<ChangeTenantUserRoleCommand>
{
    /// <summary>
    /// Inicializa as regras de validação para a alteração de papel.
    /// </summary>
    public ChangeTenantUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("O identificador do colaborador não pode ser vazio.");

        RuleFor(x => x.OperatorUserId)
            .NotEmpty()
            .WithMessage("O identificador do operador não pode ser vazio.");

        RuleFor(x => x.OperatorUserEmail)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("O email do operador deve ser válido.");

        RuleFor(x => x.NewRole)
            .IsInEnum()
            .WithMessage("O papel fornecido é inválido.");
    }
}
