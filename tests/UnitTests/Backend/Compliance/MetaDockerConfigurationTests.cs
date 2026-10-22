using FluentAssertions;

namespace UnitTests.Backend.Compliance;

/// <summary>Verifica o repasse das configurações Meta exclusivamente ao backend Docker.</summary>
public sealed class MetaDockerConfigurationTests
{
    /// <summary>Garante que ambos os ambientes entregam as configurações ao backend sem expor segredos aos frontends.</summary>
    /// <param name="fileName">Arquivo Compose sob validação.</param>
    [Theory]
    [InlineData("docker-compose.yml")]
    [InlineData("docker-compose.prod.yml")]
    public void MetaSettings_AreForwardedOnlyToWebApi(string fileName)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "docker-compose.yml")))
            root = root.Parent;

        root.Should().NotBeNull();
        var compose = File.ReadAllText(Path.Combine(root!.FullName, fileName));
        var apiStart = compose.IndexOf("  webapi:", StringComparison.Ordinal);
        var portalStart = compose.IndexOf("  webapp:", StringComparison.Ordinal);
        var api = compose[apiStart..portalStart];
        var frontends = compose[portalStart..];
        string[] settings =
        [
            "META_APP_ID", "META_APP_SECRET", "META_GRAPH_API_VERSION", "META_OAUTH_REDIRECT_URI",
            "Integrations__OAuth__StateSigningKey", "Integrations__OAuth__TokenEncryptionKey",
            "Integrations__OAuth__Meta__DefaultScopes", "Integrations__OAuth__Meta__TimeoutSeconds",
            "Integrations__MetaSynchronization__Enabled", "Integrations__MetaSynchronization__IntervalMinutes",
            "Integrations__MetaSynchronization__LookbackDays"
        ];

        foreach (var setting in settings)
        {
            api.Should().Contain($"- {setting}=${{{setting}");
            frontends.Should().NotContain(setting);
        }
    }
}
