using System.Security.Cryptography;
using System.Text;

namespace SocialPublishing.Application.Traction;

/// <summary>
/// Calcula identificadores estáveis de mídia orgânica para o Creative Hub.
/// </summary>
public static class OrganicMediaFingerprint
{
    /// <summary>
    /// Deriva fingerprint SHA-256 hexadecimal a partir da chave de objeto de mídia.
    /// </summary>
    /// <param name="objectKey">Chave do objeto no armazenamento; nula ou vazia produz nulo.</param>
    /// <returns>Hash em minúsculas ou nulo quando a chave não está disponível.</returns>
    public static string? FromObjectKey(string? objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(objectKey.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
