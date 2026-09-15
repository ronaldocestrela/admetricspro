namespace UnitTests.Backend.Common;

/// <summary>
/// Manipulador HTTP de teste que permite simular respostas da rede em chamadas aos adaptadores de API externa.
/// </summary>
public sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TestHttpMessageHandler"/>.
    /// </summary>
    /// <param name="handler">Função delegada para produzir a resposta HTTP.</param>
    public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TestHttpMessageHandler"/> com função síncrona.
    /// </summary>
    /// <param name="handler">Função delegada síncrona.</param>
    public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = (req, ct) => Task.FromResult(handler(req, ct));
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handler(request, cancellationToken);
    }
}
