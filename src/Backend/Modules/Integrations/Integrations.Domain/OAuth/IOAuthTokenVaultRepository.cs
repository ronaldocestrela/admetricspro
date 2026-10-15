namespace Integrations.Domain.OAuth;

/// <summary>
/// Contrato de repositório para consulta e persistência das credenciais do Token Vault
/// no banco de dados dedicado do inquilino.
/// </summary>
public interface IOAuthTokenVaultRepository
{
    /// <summary>Registra o hash de um state pendente no banco do tenant.</summary>
    /// <param name="state">State assinado.</param>
    /// <param name="expiresAtUtc">Validade UTC.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Tarefa de inclusão, consolidada pelo UnitOfWork.</returns>
    Task RegisterStateAsync(string state, DateTime expiresAtUtc, CancellationToken cancellationToken);

    /// <summary>Consome atomicamente o state antes da troca de código, impedindo replay entre instâncias.</summary>
    /// <param name="state">State assinado.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>True somente para o primeiro consumo dentro da validade.</returns>
    Task<bool> ConsumeStateAsync(string state, CancellationToken cancellationToken);
    /// <summary>
    /// Obtém o registro de credenciais ativas para um determinado workspace e plataforma.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="platform">Plataforma de anúncios.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O registro se encontrado; caso contrário, nulo.</returns>
    Task<OAuthTokenVault?> GetByWorkspaceAndPlatformAsync(
        Guid workspaceId,
        string platform,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as conexões armazenadas no cofre para um workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de credenciais registradas.</returns>
    Task<IReadOnlyList<OAuthTokenVault>> GetByWorkspaceIdAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os registros de tokens que expiram antes do limite temporal informado (para renovação preventiva).
    /// </summary>
    /// <param name="threshold">Janela de antecedência de expiração.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção de tokens a renovar.</returns>
    Task<IReadOnlyList<OAuthTokenVault>> GetExpiringTokensAsync(
        TimeSpan threshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo registro ao cofre de credenciais.
    /// </summary>
    /// <param name="vault">Entidade com tokens cifrados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(OAuthTokenVault vault, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza um registro existente no cofre.
    /// </summary>
    /// <param name="vault">Instância modificada.</param>
    void Update(OAuthTokenVault vault);

    /// <summary>
    /// Remove o registro de credenciais do banco.
    /// </summary>
    /// <param name="vault">Instância a ser removida.</param>
    void Remove(OAuthTokenVault vault);
}
