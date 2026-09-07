using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Domain.Primitives;

namespace WebApp.Models;

/// <summary>
/// Modelo de entrada de dados e validação do formulário de login de inquilinos.
/// </summary>
public sealed class TenantLoginModel
{
    /// <summary>
    /// Endereço de e-mail corporativo do usuário.
    /// </summary>
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Senha de acesso do usuário.
    /// </summary>
    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve conter no mínimo 6 caracteres.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Subdomínio identificador da agência/inquilino.
    /// </summary>
    public string? Subdomain { get; set; }

    /// <summary>
    /// Indica se a sessão do usuário deve ser memorizada para acessos futuros.
    /// </summary>
    public bool RememberMe { get; set; }

    /// <summary>
    /// Valida os campos do modelo retornando o padrão <see cref="Result"/>.
    /// </summary>
    /// <returns>Resultado de validação bem-sucedido ou falha com erro descritivo.</returns>
    public Result Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return Result.Failure(Error.Validation("Login.EmailRequired", "Por favor, informe seu e-mail corporativo."));
        }

        if (!Email.Contains('@') || !Email.Contains('.'))
        {
            return Result.Failure(Error.Validation("Login.EmailInvalid", "Informe um formato de e-mail válido."));
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            return Result.Failure(Error.Validation("Login.PasswordRequired", "Por favor, informe sua senha de acesso."));
        }

        if (Password.Length < 6)
        {
            return Result.Failure(Error.Validation("Login.PasswordTooShort", "A senha deve conter no mínimo 6 caracteres."));
        }

        return Result.Success();
    }
}
