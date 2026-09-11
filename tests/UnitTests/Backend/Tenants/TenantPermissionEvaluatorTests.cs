using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Rbac.Models;
using Tenants.Application.Rbac.Services;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Squads.Services;
using Tenants.Application.Users.Repositories;
using Tenants.Infrastructure.Rbac;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Suíte de testes unitários para <see cref="TenantPermissionEvaluator"/> validando a matriz completa de perfis (RBAC)
/// e as regras de governança para Owner, Admin, SquadLeader, MediaManager (com teto orçamentário), Analyst e Client Guest.
/// </summary>
public sealed class TenantPermissionEvaluatorTests
{
    private readonly ITenantUserRepository _userRepository = Substitute.For<ITenantUserRepository>();
    private readonly IUserPortfolioService _portfolioService = Substitute.For<IUserPortfolioService>();
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();
    private readonly TenantPermissionEvaluator _evaluator;

    private const decimal DefaultMaxBudgetForMediaManager = 25000m;

    /// <summary>
    /// Inicializa a suíte com as dependências mockadas e teto padrão.
    /// </summary>
    public TenantPermissionEvaluatorTests()
    {
        _evaluator = new TenantPermissionEvaluator(
            _userRepository,
            _portfolioService,
            _squadRepository,
            maxBudgetLimitForMediaManager: DefaultMaxBudgetForMediaManager);
    }

    /// <summary>
    /// Valida que o papel Owner possui autorização irrestrita sobre todas as permissões do inquilino.
    /// </summary>
    [Fact]
    public async Task HasPermission_QuandoUsuarioOwner_DevePermitirAcessoIrrestritoInclusiveBilling()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = TenantUser.Create(ownerId, "Dono Geral", "owner@agencia.com", null, "hash", TenantRole.Owner).Value;
        _userRepository.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(owner);

        // Act & Assert para todas as permissões
        foreach (TenantPermission permission in Enum.GetValues<TenantPermission>())
        {
            var result = await _evaluator.HasPermissionAsync(ownerId, permission);
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue($"Owner deve ter a permissão {permission}");
        }
    }

    /// <summary>
    /// Valida que o papel Admin possui acesso administrativo amplo, exceto controle de faturamento raiz do inquilino.
    /// </summary>
    [Fact]
    public async Task HasPermission_QuandoUsuarioAdmin_DevePermitirTudoExcetoBillingExclusivoDoOwner()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var admin = TenantUser.Create(adminId, "Administrador Geral", "admin@agencia.com", null, "hash", TenantRole.Admin).Value;
        _userRepository.GetByIdAsync(adminId, Arg.Any<CancellationToken>()).Returns(admin);

        // Act
        var billingResult = await _evaluator.HasPermissionAsync(adminId, TenantPermission.ManageBilling);
        var settingsResult = await _evaluator.HasPermissionAsync(adminId, TenantPermission.ManageSettings);
        var editResult = await _evaluator.HasPermissionAsync(adminId, TenantPermission.EditCampaigns);
        var auditResult = await _evaluator.HasPermissionAsync(adminId, TenantPermission.ViewAuditLogs);

        // Assert
        billingResult.Value.Should().BeFalse("Admin não deve gerenciar faturamento exclusivo de titularidade.");
        settingsResult.Value.Should().BeTrue("Admin deve gerenciar configurações.");
        editResult.Value.Should().BeTrue("Admin pode editar campanhas irrestritamente.");
        auditResult.Value.Should().BeTrue("Admin pode visualizar trilha de auditoria.");
    }

    /// <summary>
    /// Valida que o papel SquadLeader só pode operar clientes que pertençam à carteira de seus squads.
    /// </summary>
    [Fact]
    public async Task HasPermission_QuandoUsuarioSquadLeader_DevePermitirOperacaoApenasDentroDeSuaCarteira()
    {
        // Arrange
        var leaderId = Guid.NewGuid();
        var workspaceIdInPortfolio = Guid.NewGuid();
        var workspaceIdOutside = Guid.NewGuid();

        var leader = TenantUser.Create(leaderId, "Líder Alpha", "leader@agencia.com", null, "hash", TenantRole.SquadLeader).Value;
        _userRepository.GetByIdAsync(leaderId, Arg.Any<CancellationToken>()).Returns(leader);

        _portfolioService.HasAccessToWorkspaceAsync(leaderId, workspaceIdInPortfolio, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));
        _portfolioService.HasAccessToWorkspaceAsync(leaderId, workspaceIdOutside, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(false));

        // Act
        var allowedResult = await _evaluator.HasPermissionAsync(
            leaderId,
            TenantPermission.EditCampaigns,
            new PermissionContext(WorkspaceId: workspaceIdInPortfolio));

        var blockedResult = await _evaluator.HasPermissionAsync(
            leaderId,
            TenantPermission.EditCampaigns,
            new PermissionContext(WorkspaceId: workspaceIdOutside));

        var billingResult = await _evaluator.HasPermissionAsync(leaderId, TenantPermission.ManageBilling);

        // Assert
        allowedResult.Value.Should().BeTrue();
        blockedResult.Value.Should().BeFalse("Líder de Squad não pode operar clientes fora da carteira do seu time.");
        billingResult.Value.Should().BeFalse("Líder de Squad não acessa faturamento global da agência.");
    }

    /// <summary>
    /// Valida que o Gestor de Mídia pode editar campanhas até o teto orçamentário configurado e é bloqueado ao exceder o teto.
    /// </summary>
    [Fact]
    public async Task HasPermission_QuandoGestorDeMidia_DevePermitirEdicaoAteTetoConfigurado()
    {
        // Arrange
        var mediaManagerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var mediaManager = TenantUser.Create(mediaManagerId, "Gestor de Tráfego", "gestor@agencia.com", null, "hash", TenantRole.MediaManager).Value;
        _userRepository.GetByIdAsync(mediaManagerId, Arg.Any<CancellationToken>()).Returns(mediaManager);

        _portfolioService.HasAccessToWorkspaceAsync(mediaManagerId, workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        // Act - Orçamento dentro do teto (R$ 15.000 <= R$ 25.000)
        var allowedBudgetResult = await _evaluator.HasPermissionAsync(
            mediaManagerId,
            TenantPermission.EditCampaigns,
            new PermissionContext(WorkspaceId: workspaceId, ProposedBudget: 15000m));

        // Act - Orçamento acima do teto (R$ 30.000 > R$ 25.000)
        var exceededBudgetResult = await _evaluator.HasPermissionAsync(
            mediaManagerId,
            TenantPermission.EditCampaigns,
            new PermissionContext(WorkspaceId: workspaceId, ProposedBudget: 30000m));

        // Assert
        allowedBudgetResult.Value.Should().BeTrue("Gestor de mídia pode operar orçamentos dentro do seu teto.");
        exceededBudgetResult.Value.Should().BeFalse("Gestor de mídia não pode aprovar/editar orçamentos superiores ao teto de segurança.");
    }

    /// <summary>
    /// Valida que o Analista de Tráfego possui permissões estritas de leitura e é bloqueado para edição e margens de agência.
    /// </summary>
    [Fact]
    public async Task HasPermission_QuandoAnalistaDeTrafego_DevePermitirLeituraEBloquearEdicaoEMargens()
    {
        // Arrange
        var analystId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var analyst = TenantUser.Create(analystId, "Analista BI", "analyst@agencia.com", null, "hash", TenantRole.Analyst).Value;
        _userRepository.GetByIdAsync(analystId, Arg.Any<CancellationToken>()).Returns(analyst);

        _portfolioService.HasAccessToWorkspaceAsync(analystId, workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        // Act
        var viewCampaignsResult = await _evaluator.HasPermissionAsync(
            analystId,
            TenantPermission.ViewCampaigns,
            new PermissionContext(WorkspaceId: workspaceId));

        var exportReportsResult = await _evaluator.HasPermissionAsync(
            analystId,
            TenantPermission.ExportReports,
            new PermissionContext(WorkspaceId: workspaceId));

        var editResult = await _evaluator.HasPermissionAsync(
            analystId,
            TenantPermission.EditCampaigns,
            new PermissionContext(WorkspaceId: workspaceId));

        var marginsResult = await _evaluator.HasPermissionAsync(
            analystId,
            TenantPermission.ViewFinancialMargins,
            new PermissionContext(WorkspaceId: workspaceId));

        // Assert
        viewCampaignsResult.Value.Should().BeTrue("Analista pode visualizar campanhas da sua carteira.");
        exportReportsResult.Value.Should().BeTrue("Analista pode exportar relatórios.");
        editResult.Value.Should().BeFalse("Analista possui perfil estritamente de leitura (sem edição).");
        marginsResult.Value.Should().BeFalse("Analista não tem acesso a margens financeiras da agência.");
    }

    /// <summary>
    /// Valida que o Client Guest tem visualização blindada de seu próprio workspace e bloqueio estrito de margens e dados internos.
    /// </summary>
    [Fact]
    public async Task HasPermission_QuandoClientGuest_DeveBlindarMargensRegrasInternasEOutrosWorkspaces()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var myWorkspaceId = Guid.NewGuid();
        var otherWorkspaceId = Guid.NewGuid();

        var guest = TenantUser.Create(guestId, "Cliente Final VIP", "guest@cliente.com", null, "hash", TenantRole.Guest).Value;
        _userRepository.GetByIdAsync(guestId, Arg.Any<CancellationToken>()).Returns(guest);

        _portfolioService.HasAccessToWorkspaceAsync(guestId, myWorkspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));
        _portfolioService.HasAccessToWorkspaceAsync(guestId, otherWorkspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(false));

        // Act
        var viewOwnWorkspaceResult = await _evaluator.HasPermissionAsync(
            guestId,
            TenantPermission.ViewCampaigns,
            new PermissionContext(WorkspaceId: myWorkspaceId));

        var viewOtherWorkspaceResult = await _evaluator.HasPermissionAsync(
            guestId,
            TenantPermission.ViewCampaigns,
            new PermissionContext(WorkspaceId: otherWorkspaceId));

        var marginsResult = await _evaluator.HasPermissionAsync(
            guestId,
            TenantPermission.ViewFinancialMargins,
            new PermissionContext(WorkspaceId: myWorkspaceId));

        var automationsResult = await _evaluator.HasPermissionAsync(
            guestId,
            TenantPermission.ManageAutomations,
            new PermissionContext(WorkspaceId: myWorkspaceId));

        var auditLogsResult = await _evaluator.HasPermissionAsync(guestId, TenantPermission.ViewAuditLogs);

        // Assert
        viewOwnWorkspaceResult.Value.Should().BeTrue("Cliente Guest pode ver seu próprio workspace.");
        viewOtherWorkspaceResult.Value.Should().BeFalse("Cliente Guest é blindado contra visualização de outros workspaces.");
        marginsResult.Value.Should().BeFalse("Cliente Guest NUNCA pode visualizar margens financeiras, markups ou fees da agência.");
        automationsResult.Value.Should().BeFalse("Cliente Guest não acessa regras internas de automação.");
        auditLogsResult.Value.Should().BeFalse("Cliente Guest não visualiza trilha de auditoria.");
    }

    /// <summary>
    /// Valida que usuários inativos são sumariamente rejeitados para qualquer operação ou permissão.
    /// </summary>
    [Fact]
    public async Task HasPermission_QuandoUsuarioInativo_DeveRecusarQualquerPermissao()
    {
        // Arrange
        var inactiveUserId = Guid.NewGuid();
        var user = TenantUser.Create(inactiveUserId, "Usuário Desativado", "inactive@agencia.com", null, "hash", TenantRole.Owner).Value;
        user.Deactivate();
        _userRepository.GetByIdAsync(inactiveUserId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _evaluator.HasPermissionAsync(inactiveUserId, TenantPermission.ViewCampaigns);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantUser.Inactive");
    }
}
