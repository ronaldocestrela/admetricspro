namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Fornece jitter determinístico ou aleatório para backoff de publicação orgânica.
/// </summary>
public interface IOrganicPublishJitterProvider
{
    /// <summary>
    /// Obtém jitter em milissegundos para a política de retentativa.
    /// </summary>
    /// <returns>Valor não negativo em milissegundos.</returns>
    int GetJitterMilliseconds();
}
