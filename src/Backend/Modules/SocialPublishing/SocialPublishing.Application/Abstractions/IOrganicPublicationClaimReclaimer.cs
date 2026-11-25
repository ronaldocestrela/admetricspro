namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Devolve jobs orgânicos com claim expirado para retentativa sem chamar rede externa.
/// </summary>
public interface IOrganicPublicationClaimReclaimer
{
    /// <summary>
    /// Libera claims de <c>InProgress</c> mais antigos que o timeout configurado.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de jobs devolvidos a <c>RetryScheduled</c>.</returns>
    Task<int> ReclaimStaleClaimsAsync(CancellationToken cancellationToken = default);
}