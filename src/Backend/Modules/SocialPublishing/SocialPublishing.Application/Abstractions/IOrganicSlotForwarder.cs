namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Encaminha jobs orgânicos devidos para a outbox de mensageria no mesmo commit do claim.
/// </summary>
public interface IOrganicSlotForwarder
{
    /// <summary>
    /// Reivindica e grava outbox para todos os jobs devidos no tenant atual.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de jobs encaminhados com sucesso.</returns>
    Task<int> ForwardAllDueAsync(CancellationToken cancellationToken = default);
}