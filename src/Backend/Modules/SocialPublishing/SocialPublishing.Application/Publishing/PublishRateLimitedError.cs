using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Publishing;

/// <summary>
/// Erro de negócio quando a rede responde 429 ou quota esgotada, com opcional <c>Retry-After</c>.
/// </summary>
public sealed record PublishRateLimitedError : Error
{
    /// <summary>
    /// Inicializa o erro com código estável e atraso sugerido pela rede.
    /// </summary>
    /// <param name="description">Descrição legível.</param>
    /// <param name="retryAfter">Atraso sugerido pelo header Retry-After, se presente.</param>
    public PublishRateLimitedError(string description, TimeSpan? retryAfter)
        : base("Social.PublishRateLimited", description, ErrorType.Failure)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Obtém o atraso sugerido pela rede para retentativa.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
