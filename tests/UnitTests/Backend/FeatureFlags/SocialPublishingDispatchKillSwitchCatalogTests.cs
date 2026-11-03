using FluentAssertions;
using Master.Domain.FeatureFlags;

namespace UnitTests.Backend.FeatureFlags;

/// <summary>
/// Conformidade do kill switch de disparo orgânico no catálogo Master.
/// </summary>
public sealed class SocialPublishingDispatchKillSwitchCatalogTests
{
    /// <summary>
    /// A chave canônica está registrada entre as flags semeadas do MasterDb.
    /// </summary>
    [Fact]
    public void Catalog_IncludesSocialPublishingDispatchKillSwitchKey()
    {
        MasterCatalogFeatureFlagKeys.AllSeededKeys.Should().Contain(MasterCatalogFeatureFlagKeys.SocialPublishingDispatch);
    }

    /// <summary>
    /// O kill switch nasce desligado e pode ser armado com justificativa.
    /// </summary>
    [Fact]
    public void SocialPublishingDispatchKillSwitch_StartsDisengaged_AndActivates()
    {
        var killSwitch = FeatureFlag.CreateKillSwitch(
            key: MasterCatalogFeatureFlagKeys.SocialPublishingDispatch,
            name: "Kill Switch — Disparo de Publicação Orgânica",
            description: "Congela o worker de publicação orgânica agendada em todos os tenants.",
            createdBy: "system@admetricspro.com").Value;

        killSwitch.IsKillSwitch.Should().BeTrue();
        killSwitch.IsEnabled.Should().BeFalse();
        killSwitch.IsKillSwitchActive.Should().BeFalse();

        var activation = killSwitch.ActivateKillSwitch(
            "Incidente na Graph API durante janela de publicação",
            "ops@admetricspro.com",
            DateTime.UtcNow);

        activation.IsSuccess.Should().BeTrue();
        killSwitch.IsKillSwitchActive.Should().BeTrue();
    }
}
