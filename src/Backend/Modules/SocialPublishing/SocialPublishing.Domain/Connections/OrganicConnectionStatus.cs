namespace SocialPublishing.Domain.Connections;

/// <summary>
/// Status operacional de uma conexão orgânica.
/// </summary>
public enum OrganicConnectionStatus
{
    /// <summary>
    /// Conexão ativa e utilizável para publicação.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Conexão revogada localmente ou pela rede.
    /// </summary>
    Revoked = 2,
}
