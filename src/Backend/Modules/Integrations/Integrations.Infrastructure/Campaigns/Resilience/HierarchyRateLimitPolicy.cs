using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns.Resilience;

namespace Integrations.Infrastructure.Campaigns.Resilience;

/// <summary>
/// Implementação concreta de política de resiliência e controle de limites de taxa de requisições
/// com backoff exponencial truncado e jitter aleatório.
/// </summary>
public sealed class HierarchyRateLimitPolicy : IHierarchyRateLimitPolicy
{
    private readonly int _maxRetries;
    private readonly int _baseDelayMs;
    private readonly Random _random = new();

    /// <summary>
    /// Inicializa uma nova instância de <see cref="HierarchyRateLimitPolicy"/>.
    /// </summary>
    /// <param name="maxRetries">Quantidade máxima de retentativas (padrão: 3).</param>
    /// <param name="baseDelayMs">Atraso base inicial em milissegundos (padrão: 500ms).</param>
    public HierarchyRateLimitPolicy(int maxRetries = 3, int baseDelayMs = 500)
    {
        _maxRetries = maxRetries;
        _baseDelayMs = baseDelayMs;
    }

    /// <inheritdoc />
    public async Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> action,
        string platform,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        Result<T> lastResult = Result<T>.Failure(
            Error.Failure("RateLimit.Unknown", "Nenhuma tentativa foi executada."));

        for (var attempt = 0; attempt <= _maxRetries; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Result<T>.Failure(
                    Error.Failure("Operation.Cancelled", "A operação foi cancelada antes de concluir."));
            }

            lastResult = await action(cancellationToken);

            if (lastResult.IsSuccess)
            {
                return lastResult;
            }

            if (!IsRateLimitError(lastResult.Error) || attempt == _maxRetries)
            {
                return lastResult;
            }

            var delayMs = CalculateDelay(attempt);
            try
            {
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Result<T>.Failure(
                    Error.Failure("Operation.Cancelled", "Aguardando retentativa foi cancelada."));
            }
        }

        return lastResult;
    }

    private static bool IsRateLimitError(Error error)
    {
        if (string.IsNullOrWhiteSpace(error.Code))
        {
            return false;
        }

        return error.Code.StartsWith("RateLimit", StringComparison.OrdinalIgnoreCase) ||
               error.Code.Contains("Throttl", StringComparison.OrdinalIgnoreCase) ||
               error.Code.Contains("Quota", StringComparison.OrdinalIgnoreCase) ||
               error.Description.Contains("429", StringComparison.OrdinalIgnoreCase);
    }

    private int CalculateDelay(int attempt)
    {
        // Exponential backoff: baseDelay * 2^attempt + jitter
        var exponentialFactor = Math.Pow(2, attempt);
        var baseBackoff = _baseDelayMs * exponentialFactor;
        var jitter = _random.Next(0, Math.Max(1, _baseDelayMs / 2));
        var totalDelay = (int)Math.Min(baseBackoff + jitter, 10000); // Teto máximo de 10s

        return totalDelay;
    }
}
