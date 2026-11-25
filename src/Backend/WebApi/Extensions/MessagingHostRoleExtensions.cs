using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Messaging.DependencyInjection;
using WebApi.Configuration;

namespace WebApi.Extensions;

/// <summary>
/// Resolve o papel efetivo do host a partir da configuração.
/// </summary>
public static class MessagingHostRoleExtensions
{
    /// <summary>
    /// Obtém o papel validado ou falha com <c>Messaging.InvalidHostRole</c>.
    /// </summary>
    /// <param name="configuration">Configuração do host.</param>
    /// <returns>Papel operacional.</returns>
    public static MessagingHostRole GetRequiredMessagingHostRole(IConfiguration configuration)
    {
        var section = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
        var parse = MessagingHostRoleParser.Parse(section.HostRole);
        if (parse.IsFailure)
        {
            throw new InvalidOperationException(parse.Error.Description);
        }

        return parse.Value;
    }

    /// <summary>
    /// Resolve o modo MassTransit para o papel e a feature flag atuais.
    /// </summary>
    /// <param name="configuration">Configuração do host.</param>
    /// <returns>Modo de registro do transporte.</returns>
    public static AdMetricsMessagingRegistrationMode ResolveMessagingRegistrationMode(IConfiguration configuration)
    {
        var section = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
        var role = GetRequiredMessagingHostRole(configuration);
        return AdMetricsMessagingRegistrationModeResolver.Resolve(section.Enabled, role);
    }
}