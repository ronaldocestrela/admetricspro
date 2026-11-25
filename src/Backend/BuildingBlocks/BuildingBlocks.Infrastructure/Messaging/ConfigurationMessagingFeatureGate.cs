using BuildingBlocks.Application.Messaging;
using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Implementação de <see cref="IMessagingFeatureGate"/> lendo <c>Messaging:Enabled</c>.
/// </summary>
public sealed class ConfigurationMessagingFeatureGate(IConfiguration configuration) : IMessagingFeatureGate
{
    /// <inheritdoc />
    public bool IsEnabled => configuration.GetValue("Messaging:Enabled", false);
}