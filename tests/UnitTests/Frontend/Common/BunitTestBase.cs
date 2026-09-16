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
    /// Mock do serviço de cliente de RBAC e auditoria.
    /// </summary>
    protected WebApp.Services.ITenantRbacClientService TenantRbacClientService { get; }

    /// <summary>
    /// Mock do serviço de cliente de Branding e White-Label.
    /// </summary>
    protected WebApp.Services.ITenantBrandingClientService TenantBrandingClientService { get; }

    /// <summary>
    /// Mock do serviço de cliente de CNAME e domínios personalizados.
    /// </summary>
    protected WebApp.Services.ITenantCnameClientService TenantCnameClientService { get; }

    /// <summary>
    /// Mock do serviço de cliente de dashboard executivo analítico.
    /// </summary>
    protected WebApp.Services.IAnalyticsDashboardClientService AnalyticsDashboardClientService { get; }

    /// <summary>
    /// Mock do serviço de cliente de Pacing e orçamentos.
    /// </summary>
    protected WebApp.Services.IBudgetPacingClientService BudgetPacingClientService { get; }

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
        TenantRbacClientService = NSubstitute.Substitute.For<WebApp.Services.ITenantRbacClientService>();
        TenantBrandingClientService = NSubstitute.Substitute.For<WebApp.Services.ITenantBrandingClientService>();
        TenantCnameClientService = NSubstitute.Substitute.For<WebApp.Services.ITenantCnameClientService>();
        AnalyticsDashboardClientService = NSubstitute.Substitute.For<WebApp.Services.IAnalyticsDashboardClientService>();
        BudgetPacingClientService = NSubstitute.Substitute.For<WebApp.Services.IBudgetPacingClientService>();

        BudgetPacingClientService.GetPortfolioPacingAsync(NSubstitute.Arg.Any<Guid?>(), NSubstitute.Arg.Any<int?>(), NSubstitute.Arg.Any<int?>(), NSubstitute.Arg.Any<DateTime?>(), NSubstitute.Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Automations.Application.Pacing.DTOs.PortfolioPacingSummaryDto>.Success(
                new Automations.Application.Pacing.DTOs.PortfolioPacingSummaryDto()));

        TenantBrandingClientService.GetBrandingAsync(NSubstitute.Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Tenants.Application.Branding.DTOs.TenantBrandingDetailsDto>.Success(
                new Tenants.Application.Branding.DTOs.TenantBrandingDetailsDto(
                    PrimaryColor: "#2563EB",
                    SecondaryColor: "#0F172A",
                    LightLogoUrl: null,
                    DarkLogoUrl: null,
                    FaviconUrl: null,
                    UpdatedAtUtc: DateTime.UtcNow)));

        TenantCnameClientService.GetCnameDetailsAsync(NSubstitute.Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Master.Application.Tenants.Queries.GetTenantCustomDomain.TenantCustomDomainDto>.Success(
                new Master.Application.Tenants.Queries.GetTenantCustomDomain.TenantCustomDomainDto(
                    TenantId: Guid.NewGuid(),
                    CustomDomain: null,
                    ExpectedCnameTarget: "cname.admetricspro.com",
                    IsConfigured: false,
                    HasPlanSupport: true)));

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

        var defaultDashboard = new Analytics.Application.Dashboard.DTOs.ExecutiveDashboardDto(
            DateTime.UtcNow.AddDays(-6),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-13),
            DateTime.UtcNow.AddDays(-7),
            "BRL",
            new List<Analytics.Application.Dashboard.DTOs.ExecutiveMetricItemDto>
            {
                new("Spend", "Investimento Total", 1000m, 800m, 25.0m, true, "Currency"),
                new("Cpc", "Custo por Clique (CPC)", 1.50m, 2.00m, -25.0m, true, "Currency"),
                new("Cpm", "Custo por Mil Impressões (CPM)", 15.0m, 18.0m, -16.67m, true, "Currency"),
                new("Ctr", "Taxa de Cliques (CTR)", 3.5m, 2.8m, 25.0m, true, "Percentage"),
                new("Cpa", "Custo por Aquisição (CPA)", 20.0m, 25.0m, -20.0m, true, "Currency"),
                new("Roas", "Retorno sobre Ad Spend (ROAS)", 4.0m, 3.2m, 25.0m, true, "Multiplier")
            },
            new List<Analytics.Application.Dashboard.DTOs.ExecutiveTimeSeriesPointDto>
            {
                new(DateTime.UtcNow.AddDays(-6), 100m, 400m, 4.0m, 80, 2500, 5)
            },
            new List<Analytics.Application.Dashboard.DTOs.PlatformShareDto>
            {
                new("Meta", 600m, 2400m, 4.0m, 60.0m, 400, 30),
                new("Google", 400m, 1600m, 4.0m, 40.0m, 200, 20)
            },
            new List<Analytics.Application.Dashboard.DTOs.DeviceShareDto>
            {
                new("Mobile", 700m, 450, 35, 4.0m, 70.0m),
                new("Desktop", 300m, 150, 15, 4.0m, 30.0m)
            });

        AnalyticsDashboardClientService.GetExecutiveDashboardAsync(
            NSubstitute.Arg.Any<WebApp.Models.DashboardFiltersState>(),
            NSubstitute.Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<Analytics.Application.Dashboard.DTOs.ExecutiveDashboardDto>.Success(defaultDashboard));

        Services.AddSingleton<ITenantStateProvider>(TenantStateProvider);
        Services.AddSingleton<ITenantSessionStateProvider>(TenantSessionStateProvider);
        Services.AddSingleton<IImpersonationStateProvider>(ImpersonationStateProvider);
        Services.AddSingleton<WebApp.Services.IImpersonationClientService>(ImpersonationClientService);
        Services.AddSingleton<WebApp.Services.ITenantFtuxClientService>(TenantFtuxClientService);
        Services.AddSingleton<WebApp.Services.IWorkspaceClientService>(WorkspaceClientService);
        Services.AddSingleton<WebApp.Services.ITenantTeamClientService>(TenantTeamClientService);
        Services.AddSingleton<WebApp.Services.ISquadClientService>(SquadClientService);
        Services.AddSingleton<WebApp.Services.IBillingClientService>(BillingClientService);
        Services.AddSingleton<WebApp.Services.ITenantRbacClientService>(TenantRbacClientService);
        Services.AddSingleton<WebApp.Services.ITenantBrandingClientService>(TenantBrandingClientService);
        Services.AddSingleton<WebApp.Services.ITenantCnameClientService>(TenantCnameClientService);
        Services.AddSingleton<WebApp.Services.IAnalyticsDashboardClientService>(AnalyticsDashboardClientService);
        Services.AddSingleton<WebApp.Services.IBudgetPacingClientService>(BudgetPacingClientService);
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
