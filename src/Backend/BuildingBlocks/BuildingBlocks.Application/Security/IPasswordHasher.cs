namespace BuildingBlocks.Application.Security;

/// <summary>
/// Provedor de hashing seguro e verificação de senhas para inquilinos e operadores do sistema.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Gera um hash criptográfico seguro (PBKDF2/Argon2) para a senha informada.
    /// </summary>
    /// <param name="password">A senha em texto plano a ser hasheada.</param>
    /// <returns>O hash criptográfico gerado com salt dinâmico.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifica se a senha em texto plano informada é válida perante o hash seguro armazenado.
    /// </summary>
    /// <param name="hashedPassword">O hash criptográfico previamente gerado e persistido.</param>
    /// <param name="providedPassword">A senha em texto plano a ser verificada.</param>
    /// <returns><c>true</c> se a senha for válida; caso contrário, <c>false</c>.</returns>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
