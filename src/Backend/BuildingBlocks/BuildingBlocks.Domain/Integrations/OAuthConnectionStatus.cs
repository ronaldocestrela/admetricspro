namespace BuildingBlocks.Domain.Integrations;

/// <summary>
/// Status operacional de uma conexão OAuth armazenada no Token Vault.
/// </summary>
public static class OAuthConnectionStatus
{
    /// <summary>
    /// Conexão ativa com tokens válidos para chamadas de API.
    /// </summary>
    public const string Active = "Active";

    /// <summary>
    /// Conexão com access token expirado ou renovação pendente.
    /// </summary>
    public const string Expired = "Expired";

    /// <summary>
    /// Conexão revogada pelo usuário ou desautorizada no provedor.
    /// </summary>
    public const string Revoked = "Revoked";
}
