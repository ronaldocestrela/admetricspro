using FluentValidation;

namespace Tenants.Application.Auth.Commands.AuthenticateTenantUser;

/// <summary>
/// Validador das regras de entrada para o comando de autenticação de usuários de inquilino.
/// </summary>
public sealed class AuthenticateTenantUserCommandValidator : AbstractValidator<AuthenticateTenantUserCommand>
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="AuthenticateTenantUserCommandValidator"/> com regras de e-mail e senha.
    /// </summary>
    public AuthenticateTenantUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail corporativo é obrigatório.")
            .EmailAddress().WithMessage("Formato de e-mail inválido.")
            .MaximumLength(256).WithMessage("O e-mail não pode exceder 256 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(6).WithMessage("A senha deve conter no mínimo 6 caracteres.");
    }
}
