namespace UnitTests.Frontend.Common;

/// <summary>
/// Manipulador de mensagens HTTP simulado para testes de integração e unitários de clientes HTTP.
/// </summary>
public sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _handler;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TestHttpMessageHandler"/>.
    /// </summary>
    /// <param name="handler">Função de callback que intercepta a requisição e retorna a resposta simulada.</param>
    public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request, cancellationToken));
    }
}
