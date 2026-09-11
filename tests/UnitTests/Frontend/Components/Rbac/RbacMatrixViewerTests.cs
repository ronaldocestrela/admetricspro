using Bunit;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Rbac.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Rbac;
using Xunit;

namespace UnitTests.Frontend.Components.Rbac;

/// <summary>
/// Testes unitários com bUnit para o componente <see cref="RbacMatrixViewer"/>.
/// </summary>
public sealed class RbacMatrixViewerTests : BunitTestBase
{
    private readonly TenantRbacMatrixDto _sampleMatrix;

    /// <summary>
    /// Configura dados simulados para a matriz de permissões.
    /// </summary>
    public RbacMatrixViewerTests()
    {
        _sampleMatrix = new TenantRbacMatrixDto(
            Roles: new Dictionary<string, IReadOnlyList<string>>
            {
                ["Owner"] = ["ManageBilling", "ManageSettings", "ViewFinancialMargins", "ViewCampaigns"],
                ["Admin"] = ["ManageSettings", "ViewFinancialMargins", "ViewCampaigns"],
                ["Guest"] = ["ViewCampaigns"]
            },
            AllPermissions: ["ManageBilling", "ManageSettings", "ViewFinancialMargins", "ViewCampaigns"]);
    }

    /// <summary>
    /// Valida que a matriz é renderizada exibindo os papéis e o selo de blindagem para Guest em ViewFinancialMargins.
    /// </summary>
    [Fact]
    public void Render_ComMatrizPreenchida_DeveExibirTabelaESeloBlindadoParaGuest()
    {
        // Act
        var cut = Render<RbacMatrixViewer>(parameters =>
            parameters.Add(p => p.InitialMatrix, _sampleMatrix));

        // Assert
        cut.Find(".matrix-title").TextContent.Should().Contain("Matriz Canônica de Perfis e Permissões (RBAC)");
        var table = cut.Find(".rbac-table");
        table.Should().NotBeNull();

        // O papel Guest deve ter o badge Blindado para margens financeiras
        var shieldedBadges = cut.FindAll(".badge-shielded");
        shieldedBadges.Should().NotBeEmpty();
        shieldedBadges[0].TextContent.Should().Contain("Blindado");
    }
}
