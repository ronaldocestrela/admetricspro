using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Extensions;

/// <summary>
/// Métodos de extensão para configuração de instâncias do <see cref="IHttpClientBuilder"/> no frontend.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Configura o manipulador HTTP primário para ignorar a validação de certificados SSL não confiáveis
    /// em ambiente de desenvolvimento local (por exemplo, ao comunicar-se via HTTPS com certificados de desenvolvimento).
    /// </summary>
    /// <param name="builder">Instância do <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="isDevelopment">Indica se a aplicação está executando em ambiente de desenvolvimento.</param>
    /// <returns>A instância do <see cref="IHttpClientBuilder"/> configurada.</returns>
    public static IHttpClientBuilder ConfigureDevelopmentCertificateBypass(this IHttpClientBuilder builder, bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (isDevelopment)
        {
            builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        }

        return builder;
    }
}
