using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Protege e revela segredos OAuth orgânicos com envelope de propósito cifrado (AES-256 via kernel).
/// </summary>
public interface IOrganicTokenProtector
{
    /// <summary>
    /// Cifra um segredo vinculado a um propósito explícito.
    /// </summary>
    /// <param name="plainSecret">Segredo em claro.</param>
    /// <param name="purpose">Propósito autenticado (ex.: organic:Instagram:workspace:connection).</param>
    /// <returns>Ciphertext em Base64 pronto para persistência.</returns>
    string Protect(string plainSecret, string purpose);

    /// <summary>
    /// Decifra um segredo somente se o propósito informado corresponder ao envelope.
    /// </summary>
    /// <param name="cipherText">Ciphertext persistido.</param>
    /// <param name="expectedPurpose">Propósito esperado para esta conexão.</param>
    /// <returns>Segredo em claro ou falha de cofre.</returns>
    Result<string> Unprotect(string cipherText, string expectedPurpose);
}
