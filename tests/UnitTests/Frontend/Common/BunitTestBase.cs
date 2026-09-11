using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WebApp.State;

namespace UnitTests.Frontend.Common;

/// <summary>
/// Classe base para testes de componentes Blazor com o bUnit.
/// Fornece o contexto de teste configurado com serviços essenciais da aplicação, incluindo o provedor de estado de tenant.
/// </summary>
public abstract class BunitTestBase : BunitContext
{
    /// <summary>
    /// Instância do provedor de estado de tenant utilizada nos testes de renderização.
    /// </summary>
    protected TenantStateProvider TenantStateProvider { get; }

    /// <summary>
    /// Instância do provedor de estado de sessão do tenant utilizada nos testes de renderização.
    /// </summary>
    protected TenantSessionStateProvider TenantSessionStateProvider { get; }

    /// <summary>
    /// Instância do provedor de estado de impersonation utilizada nos testes.
    /// </summary>
    protected ImpersonationStateProvider ImpersonationStateProvider { get; }

    /// <summary>
    /// Mock do serviço de cliente de impersonation.
    /// </summary>
    protected WebApp.Services.IImpersonationClientService ImpersonationClientService { get; }

    /// <summary>
    /// Mock do serviço de cliente de FTUX.
    /// </summary>
    protected WebApp.Services.ITenantFtuxClientService TenantFtuxClientService { get; }

    /// <summary>
    /// Mock do serviço de cliente de Workspaces.
    /// </summary>
    protected WebApp.Services.IWorkspaceClientService WorkspaceClientService { get; }

    /// <summary>
    /// Mock do serviço de cliente de Squads/Team.
    /// </summary>
    protected WebApp.Services.ITenantTeamClientService TenantTeamClientService { get; }

    /// <summary>
    /// Mock do serviço de cliente de Squads.
    /// </summary>
    protected WebApp.Services.ISquadClientService SquadClientService { get; }

    /// <summary>
    /// Mock do serviço de cliente de Billing/Checkout.
    /// </summary>
    protected WebApp.Services.IBillingClientService BillingClientService { get; }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BunitTestBase"/> com os provedores registrados.
    /// </summary>
    protected BunitTestBase()
    {
        TenantStateProvider = new TenantStateProvider();
        TenantSessionStateProvider = new TenantSessionStateProvider(TenantStateProvider);
        ImpersonationStateProvider = new ImpersonationStateProvider();
        ImpersonationClientService = NSubstitute.Substitute.For<WebApp.Services.IImpersonationClientService>();
        TenantFtuxClientService = NSubstitute.Substitute.For<WebApp.Services.ITenantFtuxClientService>();
        WorkspaceClientService = NSubstitute.Substitute.For<WebApp.Services.IWorkspaceClientService>();
        TenantTeamClientService = NSubstitute.Substitute.For<WebApp.Services.ITenantTeamClientService>();
        SquadClientService = NSubstitute.Substitute.For<WebApp.Services.ISquadClientService>();
        BillingClientService = NSubstitute.Substitute.For<WebApp.Services.IBillingClientService>();

        TenantFtuxClientService.GetFtuxStatusAsync(NSubstitute.Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Tenants.Application.Ftux.DTOs.TenantFtuxStatusDto>.Success(
                new Tenants.Application.Ftux.DTOs.TenantFtuxStatusDto(
                    IsProvisioned: true,
                    WorkspacesCount: 0,
                    ConnectedAdAccountsCount: 0,
                    TeamMembersCount: 1,
                    SquadsCount: 0,
                    Step1Completed: true,
                    Step2Completed: false,
                    Step3Completed: false,
                    Step4Completed: false,
                    ProgressPercentage: 25,
                    IsCompleted: false)));

        TenantTeamClientService.GetUsersAsync(NSubstitute.Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<Tenants.Application.Users.DTOs.TenantUserDto>>.Success(Array.Empty<Tenants.Application.Users.DTOs.TenantUserDto>()));
        WorkspaceClientService.GetWorkspacesAsync(NSubstitute.Arg.Any<bool?>(), NSubstitute.Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<Tenants.Application.Workspaces.DTOs.WorkspaceDto>>.Success(Array.Empty<Tenants.Application.Workspaces.DTOs.WorkspaceDto>()));
        SquadClientService.GetSquadsAsync(NSubstitute.Arg.Any<bool?>(), NSubstitute.Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<Tenants.Application.Squads.DTOs.SquadSummaryDto>>.Success(Array.Empty<Tenants.Application.Squads.DTOs.SquadSummaryDto>()));

        Services.AddSingleton<ITenantStateProvider>(TenantStateProvider);
        Services.AddSingleton<ITenantSessionStateProvider>(TenantSessionStateProvider);
        Services.AddSingleton<IImpersonationStateProvider>(ImpersonationStateProvider);
        Services.AddSingleton<WebApp.Services.IImpersonationClientService>(ImpersonationClientService);
        Services.AddSingleton<WebApp.Services.ITenantFtuxClientService>(TenantFtuxClientService);
        Services.AddSingleton<WebApp.Services.IWorkspaceClientService>(WorkspaceClientService);
        Services.AddSingleton<WebApp.Services.ITenantTeamClientService>(TenantTeamClientService);
        Services.AddSingleton<WebApp.Services.ISquadClientService>(SquadClientService);
        Services.AddSingleton<WebApp.Services.IBillingClientService>(BillingClientService);
    }

    /// <summary>
    /// Altera o estado do tenant ativo no contexto do teste, disparando a notificação de mudança.
    /// </summary>
    /// <param name="tenantState">O novo estado de tenant a ser aplicado.</param>
    protected void SetTenant(TenantState tenantState)
    {
        TenantStateProvider.SetTenant(tenantState);
    }
}
