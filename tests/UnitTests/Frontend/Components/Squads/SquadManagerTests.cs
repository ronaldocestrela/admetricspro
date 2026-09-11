using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Squads.DTOs;
using Tenants.Application.Users.DTOs;
using Tenants.Application.Workspaces.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Squads;
using WebApp.Models;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Components.Squads;

/// <summary>
/// Testes unitários com bUnit para o componente <see cref="SquadManager"/>.
/// Valida listagem, métricas, filtros, criação, multi-seleção de membros e clientes, e simulador de carteira.
/// </summary>
public sealed class SquadManagerTests : BunitTestBase
{
    private readonly Guid _squad1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _squad2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _user1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _user2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _ws1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _ws2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly List<SquadSummaryDto> _sampleSquads;
    private readonly SquadDetailsDto _sampleSquadDetails;
    private readonly List<TenantUserDto> _sampleUsers;
    private readonly List<WorkspaceDto> _sampleWorkspaces;

    /// <summary>
    /// Configura dados simulados para as suítes de testes.
    /// </summary>
    public SquadManagerTests()
    {
        _sampleSquads = new List<SquadSummaryDto>
        {
            new(_squad1Id, "Squad E-commerce", "Focado em lojas virtuais", true, 2, 3, DateTime.UtcNow.AddDays(-20)),
            new(_squad2Id, "Squad Branding", "Célula institucional", false, 1, 1, DateTime.UtcNow.AddDays(-10))
        };

        _sampleSquadDetails = new SquadDetailsDto(
            _squad1Id,
            "Squad E-commerce",
            "Focado em lojas virtuais",
            true,
            new List<SquadMemberDto>
            {
                new(_user1Id, "Carlos Gestor", "carlos@agencia.com", "MediaManager", DateTime.UtcNow.AddDays(-20))
            },
            new List<SquadWorkspaceDto>
            {
                new(_ws1Id, "Loja Alpha", "12.345.678/0001-90", 15000m, "Varejo", DateTime.UtcNow.AddDays(-20))
            },
            DateTime.UtcNow.AddDays(-20),
            null);

        _sampleUsers = new List<TenantUserDto>
        {
            new(_user1Id, "Carlos Gestor", "carlos@agencia.com", null, "MediaManager", true, DateTime.UtcNow),
            new(_user2Id, "Ana Analista", "ana@agencia.com", null, "Analyst", true, DateTime.UtcNow)
        };

        _sampleWorkspaces = new List<WorkspaceDto>
        {
            new(_ws1Id, "Loja Alpha", "12.345.678/0001-90", 15000m, "Varejo", true, DateTime.UtcNow, null),
            new(_ws2Id, "Loja Beta", "98.765.432/0001-10", 8000m, "Cosméticos", true, DateTime.UtcNow, null)
        };

        SetTenant(new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Agência Vanguarda",
            Slug: "vanguarda",
            CustomDomain: null,
            Branding: TenantBranding.Default));

        SquadClientService.GetSquadsAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<SquadSummaryDto>>.Success(_sampleSquads));

        SquadClientService.GetSquadByIdAsync(_squad1Id, Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<SquadDetailsDto>.Success(_sampleSquadDetails));

        TenantTeamClientService.GetUsersAsync(Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<TenantUserDto>>.Success(_sampleUsers));

        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<WorkspaceDto>>.Success(_sampleWorkspaces));
    }

    /// <summary>
    /// Valida que ao renderizar o componente, os cards de métricas operacionais são exibidos com os totais consolidados.
    /// </summary>
    [Fact]
    public void SquadManager_WhenRendered_ShouldDisplayMetricsCards()
    {
        // Act
        var cut = Render<SquadManager>();

        // Assert
        var headerTitle = cut.Find(".page-title");
        headerTitle.TextContent.Trim().Should().Contain("Squads");

        var summaryCards = cut.FindAll(".summary-card");
        summaryCards.Should().HaveCount(4);

        // Total squads: 2
        summaryCards[0].QuerySelector(".summary-value")!.TextContent.Trim().Should().Be("2");
        // Squads ativos: 1
        summaryCards[1].QuerySelector(".summary-value")!.TextContent.Trim().Should().Be("1");
    }

    /// <summary>
    /// Valida que a lista de squads renderiza cada time cadastrado com seus badges.
    /// </summary>
    [Fact]
    public void SquadManager_WhenSquadsLoaded_ShouldRenderSquadCards()
    {
        // Act
        var cut = Render<SquadManager>();

        // Assert
        var squadCards = cut.FindAll(".squad-card");
        squadCards.Should().HaveCount(2);

        var squadNames = squadCards.Select(c => c.QuerySelector(".squad-name")!.TextContent.Trim()).ToList();
        squadNames.Should().Contain("Squad E-commerce");
        squadNames.Should().Contain("Squad Branding");
    }

    /// <summary>
    /// Valida que quando não há squads cadastrados, o estado vazio com call-to-action é exibido.
    /// </summary>
    [Fact]
    public void SquadManager_WhenNoSquads_ShouldRenderEmptyState()
    {
        // Arrange
        SquadClientService.GetSquadsAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<SquadSummaryDto>>.Success(Array.Empty<SquadSummaryDto>()));

        // Act
        var cut = Render<SquadManager>();

        // Assert
        var emptyState = cut.Find(".empty-state");
        emptyState.Should().NotBeNull();
        emptyState.TextContent.Should().Contain("Nenhum squad encontrado");
    }

    /// <summary>
    /// Valida que a busca em tempo real filtra os squads por nome.
    /// </summary>
    [Fact]
    public void SquadManager_SearchFilter_ShouldFilterSquadsByName()
    {
        // Act
        var cut = Render<SquadManager>();

        var searchInput = cut.Find(".search-input");
        searchInput.Input("Branding");

        // Assert
        var squadCards = cut.FindAll(".squad-card");
        squadCards.Should().HaveCount(1);
        squadCards[0].QuerySelector(".squad-name")!.TextContent.Trim().Should().Be("Squad Branding");
    }

    /// <summary>
    /// Valida a abertura do modal de criação de squad e submissão com sucesso.
    /// </summary>
    [Fact]
    public void SquadManager_CreateSquad_ShouldCallClientService()
    {
        // Arrange
        var newSquadId = Guid.NewGuid();
        SquadClientService.CreateSquadAsync(Arg.Any<CreateSquadModel>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Guid>.Success(newSquadId));

        var cut = Render<SquadManager>();

        // Act - Abre modal
        var createBtn = cut.Find("#btn-open-create-squad");
        createBtn.Click();

        // Preenche campos
        var nameInput = cut.Find("#input-squad-name");
        nameInput.Change("Squad Performance Growth");

        var descInput = cut.Find("#input-squad-desc");
        descInput.Change("Célula dedicada a aceleração de contas");

        // Submete
        var submitBtn = cut.Find("#btn-submit-squad");
        submitBtn.Click();

        // Assert
        SquadClientService.Received(1).CreateSquadAsync(
            Arg.Is<CreateSquadModel>(m => m.Name == "Squad Performance Growth"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ao clicar em gerenciar um squad, o drawer/modal de membros e carteira é aberto.
    /// </summary>
    [Fact]
    public void SquadManager_ManageSquad_ShouldDisplayDetailsAndMembers()
    {
        // Act
        var cut = Render<SquadManager>();

        var manageBtn = cut.Find($".btn-manage-squad[data-squad-id='{_squad1Id}']");
        manageBtn.Click();

        // Assert
        var drawer = cut.Find(".squad-manage-modal");
        drawer.Should().NotBeNull();
        drawer.QuerySelector(".manage-title")!.TextContent.Should().Contain("Squad E-commerce");
    }

    /// <summary>
    /// Valida a multi-seleção de membros para inclusão em lote no squad.
    /// </summary>
    [Fact]
    public void SquadManager_MultiSelectMembers_ShouldCallServiceForEachSelectedMember()
    {
        // Arrange
        SquadClientService.AddSquadMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result.Success());

        var cut = Render<SquadManager>();

        // Abre gerenciamento
        var manageBtn = cut.Find($".btn-manage-squad[data-squad-id='{_squad1Id}']");
        manageBtn.Click();

        // Marca o usuário disponível (Ana Analista)
        var memberCheckbox = cut.Find($".member-select-checkbox[data-user-id='{_user2Id}']");
        memberCheckbox.Change(true);

        // Clica no botão de vincular membros selecionados
        var assignMembersBtn = cut.Find("#btn-assign-selected-members");
        assignMembersBtn.Click();

        // Assert
        SquadClientService.Received(1).AddSquadMemberAsync(_squad1Id, _user2Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida a multi-seleção de workspaces para alocação em lote à carteira do squad.
    /// </summary>
    [Fact]
    public void SquadManager_MultiSelectWorkspaces_ShouldCallServiceForEachSelectedWorkspace()
    {
        // Arrange
        SquadClientService.AssignSquadWorkspaceAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result.Success());

        var cut = Render<SquadManager>();

        // Abre gerenciamento
        var manageBtn = cut.Find($".btn-manage-squad[data-squad-id='{_squad1Id}']");
        manageBtn.Click();

        // Troca para aba de Workspaces
        var wsTabBtn = cut.Find("#tab-btn-workspaces");
        wsTabBtn.Click();

        // Marca o workspace disponível (Loja Beta)
        var wsCheckbox = cut.Find($".ws-select-checkbox[data-workspace-id='{_ws2Id}']");
        wsCheckbox.Change(true);

        // Clica no botão de alocar selecionados
        var assignWsBtn = cut.Find("#btn-assign-selected-workspaces");
        assignWsBtn.Click();

        // Assert
        SquadClientService.Received(1).AssignSquadWorkspaceAsync(_squad1Id, _ws2Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida a remoção de um colaborador do squad.
    /// </summary>
    [Fact]
    public void SquadManager_RemoveMember_ShouldCallClientService()
    {
        // Arrange
        SquadClientService.RemoveSquadMemberAsync(_squad1Id, _user1Id, Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result.Success());

        var cut = Render<SquadManager>();

        var manageBtn = cut.Find($".btn-manage-squad[data-squad-id='{_squad1Id}']");
        manageBtn.Click();

        // Clica para remover o membro
        var removeBtn = cut.Find($".btn-remove-member[data-user-id='{_user1Id}']");
        removeBtn.Click();

        // Assert
        SquadClientService.Received(1).RemoveSquadMemberAsync(_squad1Id, _user1Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida a desassociação de um workspace da carteira do squad.
    /// </summary>
    [Fact]
    public void SquadManager_UnassignWorkspace_ShouldCallClientService()
    {
        // Arrange
        SquadClientService.UnassignSquadWorkspaceAsync(_squad1Id, _ws1Id, Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result.Success());

        var cut = Render<SquadManager>();

        var manageBtn = cut.Find($".btn-manage-squad[data-squad-id='{_squad1Id}']");
        manageBtn.Click();

        // Aba Workspaces
        var wsTabBtn = cut.Find("#tab-btn-workspaces");
        wsTabBtn.Click();

        // Clica para desassociar
        var unassignBtn = cut.Find($".btn-unassign-workspace[data-workspace-id='{_ws1Id}']");
        unassignBtn.Click();

        // Assert
        SquadClientService.Received(1).UnassignSquadWorkspaceAsync(_squad1Id, _ws1Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida o funcionamento do Simulador de Carteira (Portfolio Inspector) ao consultar os clientes autorizados de um operador.
    /// </summary>
    [Fact]
    public void SquadManager_PortfolioInspector_ShouldDisplayAccessibleWorkspaces()
    {
        // Arrange
        SquadClientService.GetUserPortfolioAsync(_user1Id, Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<WorkspaceDto>>.Success(
                new List<WorkspaceDto> { _sampleWorkspaces[0] }));

        var cut = Render<SquadManager>();

        // Abre o inspetor
        var openInspectorBtn = cut.Find("#btn-open-portfolio-inspector");
        openInspectorBtn.Click();

        // Seleciona o usuário
        var selectUser = cut.Find("#select-portfolio-user");
        selectUser.Change(_user1Id.ToString());

        // Assert
        var authorizedList = cut.Find(".portfolio-authorized-list");
        authorizedList.Should().NotBeNull();
        authorizedList.TextContent.Should().Contain("Loja Alpha");
    }
}
