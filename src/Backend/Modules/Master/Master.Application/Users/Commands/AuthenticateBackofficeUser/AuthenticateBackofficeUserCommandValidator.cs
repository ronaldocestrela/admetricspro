using FluentValidation;

namespace Master.Application.Users.Commands.AuthenticateBackofficeUser;

/// <summary>
/// Validador de dados de entrada para o comando de autenticação do Backoffice.
/// </summary>
public sealed class AuthenticateBackofficeUserCommandValidator : AbstractValidator<AuthenticateBackofficeUserCommand>
{
    /// <summary>
    /// Inicializa as regras de validação para campos de credenciais.
    /// </summary>
    public AuthenticateBackofficeUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("Formato de e-mail inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.");
    }
}
