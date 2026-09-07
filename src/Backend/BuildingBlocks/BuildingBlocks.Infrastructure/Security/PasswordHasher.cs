using BuildingBlocks.Application.Security;
using Microsoft.AspNetCore.Identity;

namespace BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Implementação robusta de <see cref="IPasswordHasher"/> baseada no algoritmo PBKDF2 com derivação HMAC-SHA256 e salting dinâmico.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _hasher;
    private static readonly object DummyUser = new();

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PasswordHasher"/>.
    /// </summary>
    public PasswordHasher()
    {
        _hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>();
    }

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("A senha não pode ser nula ou vazia para geração de hash.", nameof(password));
        }

        return _hasher.HashPassword(DummyUser, password);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        try
        {
            var result = _hasher.VerifyHashedPassword(DummyUser, hashedPassword, providedPassword);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch
        {
            return false;
        }
    }
}
