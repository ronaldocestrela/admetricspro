using BuildingBlocks.Domain.Primitives;

namespace Tenants.Application.Users.Services;

/// <summary>
/// Política de complexidade de senha para colaboradores do inquilino.
/// </summary>
public static class TenantPasswordPolicy
{
    /// <summary>
    /// Valida se a senha atende aos requisitos mínimos de segurança.
    /// </summary>
    /// <param name="password">Senha em texto plano.</param>
    /// <returns>Resultado de sucesso ou erro de validação.</returns>
    public static Result Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure(
                Error.Validation("TenantUser.InvalidPassword", "A senha é obrigatória."));
        }

        if (password.Length < 8)
        {
            return Result.Failure(
                Error.Validation("TenantUser.WeakPassword", "A senha precisa de 8 caracteres, letras maiúsculas e minúsculas, número e símbolo."));
        }

        if (!password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) ||
            !password.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
        {
            return Result.Failure(
                Error.Validation("TenantUser.WeakPassword", "A senha precisa de 8 caracteres, letras maiúsculas e minúsculas, número e símbolo."));
        }

        return Result.Success();
    }
}