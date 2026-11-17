using System.Security.Cryptography;
using System.Text;

namespace SocialPublishing.Domain.Approvals;

/// <summary>
/// Utilitário para hash SHA-256 de tokens opacos de aprovação pública.
/// </summary>
public static class ApprovalTokenHasher
{
    /// <summary>
    /// Calcula o hash SHA-256 hexadecimal (64 caracteres) dos bytes brutos do token.
    /// </summary>
    /// <param name="tokenBytes">Bytes do token opaco (tipicamente 32 bytes).</param>
    /// <returns>Representação hexadecimal em maiúsculas.</returns>
    public static string HashBytes(ReadOnlySpan<byte> tokenBytes) =>
        Convert.ToHexString(SHA256.HashData(tokenBytes));

    /// <summary>
    /// Decodifica um token Base64Url e calcula seu hash SHA-256.
    /// </summary>
    /// <param name="plainToken">Token em claro recebido na URL.</param>
    /// <param name="tokenBytes">Bytes decodificados quando bem-sucedido.</param>
    /// <returns>Hash hexadecimal ou <c>null</c> se o token não for decodificável.</returns>
    public static string? TryHashPlainToken(string plainToken, out byte[]? tokenBytes)
    {
        tokenBytes = null;
        if (string.IsNullOrWhiteSpace(plainToken))
        {
            return null;
        }

        try
        {
            var normalized = plainToken.Trim().Replace('-', '+').Replace('_', '/');
            switch (normalized.Length % 4)
            {
                case 2: normalized += "=="; break;
                case 3: normalized += "="; break;
            }

            tokenBytes = Convert.FromBase64String(normalized);
            if (tokenBytes.Length != 32)
            {
                tokenBytes = null;
                return null;
            }

            return HashBytes(tokenBytes);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Codifica bytes aleatórios como token Base64Url sem padding.
    /// </summary>
    /// <param name="tokenBytes">32 bytes de entropia.</param>
    /// <returns>Token opaco para a URL.</returns>
    public static string ToBase64UrlToken(ReadOnlySpan<byte> tokenBytes)
    {
        var base64 = Convert.ToBase64String(tokenBytes);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Gera 32 bytes criptograficamente seguros e retorna o token Base64Url correspondente.
    /// </summary>
    /// <param name="tokenBytes">Bytes gerados.</param>
    /// <returns>Token opaco para entrega única ao cliente autenticado.</returns>
    public static string GeneratePlainToken(out byte[] tokenBytes)
    {
        tokenBytes = new byte[32];
        RandomNumberGenerator.Fill(tokenBytes);
        return ToBase64UrlToken(tokenBytes);
    }
}
