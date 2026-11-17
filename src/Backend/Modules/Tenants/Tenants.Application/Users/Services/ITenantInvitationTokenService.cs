namespace Tenants.Application.Users.Services;

/// <summary>
/// Serviço para geração e hash de tokens opacos de convite.
/// </summary>
public interface ITenantInvitationTokenService
{
    /// <summary>
    /// Gera um token opaco e seu hash SHA-256 para persistência.
    /// </summary>
    /// <returns>Tupla com token em texto plano e hash persistido.</returns>
    (string RawToken, string TokenHash) Generate();

    /// <summary>
    /// Calcula o hash SHA-256 de um token informado.
    /// </summary>
    /// <param name="rawToken">Token opaco recebido na URL.</param>
    /// <returns>Hash em hexadecimal minúsculo.</returns>
    string Hash(string rawToken);
}