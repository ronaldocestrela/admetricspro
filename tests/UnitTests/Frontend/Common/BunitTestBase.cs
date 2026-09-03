using Bunit;
using Microsoft.Extensions.DependencyInjection;
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
    /// Instância do provedor de estado de impersonation utilizada nos testes.
    /// </summary>
    protected ImpersonationStateProvider ImpersonationStateProvider { get; }

    /// <summary>
    /// Mock do serviço de cliente de impersonation.
    /// </summary>
    protected WebApp.Services.IImpersonationClientService ImpersonationClientService { get; }

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BunitTestBase"/> com os provedores registrados.
    /// </summary>
    protected BunitTestBase()
    {
        TenantStateProvider = new TenantStateProvider();
        ImpersonationStateProvider = new ImpersonationStateProvider();
        ImpersonationClientService = NSubstitute.Substitute.For<WebApp.Services.IImpersonationClientService>();

        Services.AddSingleton<ITenantStateProvider>(TenantStateProvider);
        Services.AddSingleton<IImpersonationStateProvider>(ImpersonationStateProvider);
        Services.AddSingleton<WebApp.Services.IImpersonationClientService>(ImpersonationClientService);
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
