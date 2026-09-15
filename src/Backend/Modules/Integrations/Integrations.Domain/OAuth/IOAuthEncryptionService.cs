namespace Integrations.Domain.OAuth;

/// <summary>
/// Contrato de serviço para cifragem e decifragem simétrica (AES-256) das credenciais sensíveis armazenadas no Token Vault.
/// </summary>
public interface IOAuthEncryptionService
{
    /// <summary>
    /// Criptografa o token em texto claro retornando payload Base64 cifrado com AES-256 e IV aleatório.
    /// </summary>
    /// <param name="plainText">Token em texto claro.</param>
    /// <returns>String cifrada em Base64.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Descriptografa o payload Base64 cifrado retornando o token em texto claro original.
    /// </summary>
    /// <param name="cipherText">String cifrada em Base64.</param>
    /// <returns>Token em texto claro.</returns>
    string Decrypt(string cipherText);
}
