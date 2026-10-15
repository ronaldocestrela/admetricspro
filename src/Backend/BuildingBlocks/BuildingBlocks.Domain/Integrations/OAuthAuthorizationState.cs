namespace BuildingBlocks.Domain.Integrations;

/// <summary>Registro de uso único de um state OAuth; contém somente seu hash e validade.</summary>
public sealed class OAuthAuthorizationState
{
    /// <summary>Hash SHA-256 do state assinado, sem código ou credenciais.</summary>
    public string StateHash { get; private set; } = string.Empty;

    /// <summary>Instante UTC após o qual a autorização não pode ser consumida.</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    private OAuthAuthorizationState() { }

    /// <summary>Inicializa o registro de autorização pendente.</summary>
    /// <param name="stateHash">Hash do state aleatório.</param>
    /// <param name="expiresAtUtc">Validade UTC.</param>
    public OAuthAuthorizationState(string stateHash, DateTime expiresAtUtc)
    {
        StateHash = stateHash;
        ExpiresAtUtc = expiresAtUtc;
    }
}
