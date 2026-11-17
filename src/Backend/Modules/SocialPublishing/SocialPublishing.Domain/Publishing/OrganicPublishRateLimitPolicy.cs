namespace SocialPublishing.Domain.Publishing;

/// <summary>
/// Política pura de backoff exponencial com jitter para retentativa de publicação orgânica (ADR 0027, local ao módulo).
/// </summary>
public sealed class OrganicPublishRateLimitPolicy
{
    private readonly int _baseDelayMs;
    private readonly int _maxDelayMs;
    private readonly int _maxAttempts;

    /// <summary>
    /// Inicializa a política com limites configuráveis.
    /// </summary>
    /// <param name="baseDelayMs">Atraso base em milissegundos.</param>
    /// <param name="maxDelayMs">Teto de atraso em milissegundos.</param>
    /// <param name="maxAttempts">Número máximo de tentativas de publicação por job.</param>
    public OrganicPublishRateLimitPolicy(int baseDelayMs = 500, int maxDelayMs = 10_000, int maxAttempts = 3)
    {
        if (baseDelayMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseDelayMs));
        }

        if (maxDelayMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDelayMs));
        }

        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        }

        _baseDelayMs = baseDelayMs;
        _maxDelayMs = maxDelayMs;
        _maxAttempts = maxAttempts;
    }

    /// <summary>
    /// Calcula o atraso em milissegundos: <c>min(2^attempt * baseDelay + jitter, maxDelay)</c>.
    /// </summary>
    /// <param name="attempt">Índice da tentativa (0-based).</param>
    /// <param name="jitterMs">Jitter determinístico injetado pelo chamador (testes) ou sorteado pelo worker.</param>
    /// <returns>Atraso total em milissegundos.</returns>
    public int CalculateDelayMs(int attempt, int jitterMs)
    {
        if (attempt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt));
        }

        if (jitterMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(jitterMs));
        }

        var exponential = _baseDelayMs * Math.Pow(2, attempt);
        var withJitter = (int)Math.Min(exponential + jitterMs, _maxDelayMs);
        return withJitter;
    }

    /// <summary>
    /// Indica se ainda restam tentativas antes do teto configurado.
    /// </summary>
    /// <param name="attemptCount">Quantidade de tentativas já realizadas.</param>
    /// <returns><c>true</c> quando <paramref name="attemptCount"/> &lt; <see cref="MaxAttempts"/>.</returns>
    public bool HasAttemptsRemaining(int attemptCount) => attemptCount < _maxAttempts;

    /// <summary>
    /// Obtém o número máximo de tentativas configurado.
    /// </summary>
    public int MaxAttempts => _maxAttempts;
}
