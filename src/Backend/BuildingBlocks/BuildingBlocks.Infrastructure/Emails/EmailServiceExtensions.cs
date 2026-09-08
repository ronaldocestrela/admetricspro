using BuildingBlocks.Application.Emails;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Emails;

/// <summary>
/// Métodos de extensão para registro de serviços de mensageria de e-mails transacionais.
/// </summary>
public static class EmailServiceExtensions
{
    /// <summary>
    /// Adiciona a infraestrutura de envio de e-mails transacionais ao contêiner de injeção de dependências.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A coleção de serviços para encadeamento fluente.</returns>
    public static IServiceCollection AddEmailInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();

        if (string.Equals(emailOptions.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, InMemoryEmailSender>();
        }

        return services;
    }
}
