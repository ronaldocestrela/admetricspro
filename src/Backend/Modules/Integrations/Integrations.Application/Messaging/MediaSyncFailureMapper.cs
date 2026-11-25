using BuildingBlocks.Domain.Primitives;

namespace Integrations.Application.Messaging;

/// <summary>
/// Traduz erros de sincronização para a política de ack de mensageria.
/// </summary>
public static class MediaSyncFailureMapper
{
    /// <summary>
    /// Converte falhas de rate limit em <see cref="ITransientIntegrationFailure"/> para reagendamento.
    /// </summary>
    /// <param name="result">Resultado da execução de sync.</param>
    /// <param name="retryAfterUtc">Próximo instante quando a rede não informa Retry-After.</param>
    /// <returns>Resultado inalterado ou com erro transitório.</returns>
    public static Result ForDisposition(Result result, DateTimeOffset retryAfterUtc)
    {
        if (result.IsSuccess)
        {
            return result;
        }

        if (!IsRateLimit(result.Error))
        {
            return result;
        }

        return Result.Failure(new MediaSyncTransientFailure(
            result.Error.Code,
            result.Error.Description,
            retryAfterUtc));
    }

    private static bool IsRateLimit(Error error)
    {
        if (string.IsNullOrWhiteSpace(error.Code))
        {
            return false;
        }

        return error.Code.StartsWith("RateLimit", StringComparison.OrdinalIgnoreCase)
               || error.Code.Contains("Throttl", StringComparison.OrdinalIgnoreCase)
               || error.Code.Contains("Quota", StringComparison.OrdinalIgnoreCase)
               || string.Equals(error.Code, "Meta.RateLimited", StringComparison.OrdinalIgnoreCase)
               || error.Description.Contains("429", StringComparison.OrdinalIgnoreCase);
    }
}