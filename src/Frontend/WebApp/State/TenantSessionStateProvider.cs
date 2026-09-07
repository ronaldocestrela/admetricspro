using Tenants.Application.Auth.DTOs;

namespace WebApp.State;

/// <summary>
/// Implementação concreta do provedor de estado de sessão do inquilino (<see cref="ITenantSessionStateProvider"/>).
/// Registrado com ciclo de vida Scoped por circuito SignalR.
/// </summary>
public sealed class TenantSessionStateProvider : ITenantSessionStateProvider
{
    private readonly ITenantStateProvider _tenantStateProvider;
    private AuthenticatedTenantUserDto? _currentSession;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantSessionStateProvider"/>.
    /// </summary>
    /// <param name="tenantStateProvider">Provedor de estado institucional e de marca do inquilino.</param>
    public TenantSessionStateProvider(ITenantStateProvider tenantStateProvider)
    {
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public bool IsAuthenticated => _currentSession is not null;

    /// <inheritdoc />
    public AuthenticatedTenantUserDto? CurrentSession => _currentSession;

    /// <inheritdoc />
    public event Action? OnSessionChanged;

    /// <inheritdoc />
    public void SetSession(AuthenticatedTenantUserDto session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _currentSession = session;

        var primary = session.Branding?.PrimaryColor ?? "#4f46e5";
        var secondary = session.Branding?.SecondaryColor ?? "#0f172a";
        var companyName = session.Branding?.AgencyName ?? session.Subdomain;

        var branding = new TenantBranding(
            PrimaryColor: primary,
            SecondaryColor: secondary,
            AccentColor: "#38bdf8",
            LogoUrl: session.Branding?.LightLogoUrl,
            DarkLogoUrl: session.Branding?.DarkLogoUrl,
            FaviconUrl: session.Branding?.FaviconUrl,
            CompanyName: companyName,
            ShowPoweredBy: true);

        var tenantState = new TenantState(
            TenantId: session.TenantId,
            Name: companyName,
            Slug: session.Subdomain,
            CustomDomain: null,
            Branding: branding);

        _tenantStateProvider.SetTenant(tenantState);
        OnSessionChanged?.Invoke();
    }

    /// <inheritdoc />
    public void ClearSession()
    {
        _currentSession = null;
        _tenantStateProvider.SetTenant(TenantState.Default);
        OnSessionChanged?.Invoke();
    }
}
