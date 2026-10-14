using Tenants.Application.Auth.DTOs;

namespace WebApp.State;

/// <summary>
/// Implementação concreta do provedor de estado de sessão do inquilino (<see cref="ITenantSessionStateProvider"/>).
/// Registrado com ciclo de vida Scoped por circuito SignalR, mantendo persistência sincronizada no navegador.
/// </summary>
public sealed class TenantSessionStateProvider : ITenantSessionStateProvider
{
    private const string StorageKey = "admetricspro_tenant_session";
    private readonly ITenantStateProvider _tenantStateProvider;
    private readonly IBrowserStorageService? _storage;
    private AuthenticatedTenantUserDto? _currentSession;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantSessionStateProvider"/>.
    /// </summary>
    /// <param name="tenantStateProvider">Provedor de estado institucional e de marca do inquilino.</param>
    /// <param name="storage">Serviço de armazenamento no navegador.</param>
    public TenantSessionStateProvider(
        ITenantStateProvider tenantStateProvider,
        IBrowserStorageService? storage = null)
    {
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
        _storage = storage;
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
        ApplySession(session);

        if (_storage != null)
        {
            _ = _storage.SetItemAsync(StorageKey, session).AsTask().ContinueWith(
                _ => { },
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    /// <inheritdoc />
    public async Task SetSessionAsync(AuthenticatedTenantUserDto session, CancellationToken cancellationToken = default)
    {
        ApplySession(session);

        if (_storage != null)
        {
            try
            {
                await _storage.SetItemAsync(StorageKey, session, cancellationToken);
            }
            catch
            {
                // Tratamento gracioso em caso de restrições de JSInterop durante pré-render
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> RestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_storage == null)
        {
            return false;
        }

        try
        {
            var stored = await _storage.GetItemAsync<AuthenticatedTenantUserDto>(StorageKey, cancellationToken);
            if (stored is not null && stored.TenantId != Guid.Empty)
            {
                ApplySession(stored);
                return true;
            }
        }
        catch
        {
            // Tratamento gracioso caso o JSInterop ainda não esteja disponível ou dados corrompidos
        }

        return false;
    }

    /// <inheritdoc />
    public void ClearSession() => _ = ClearSessionAsync();

    /// <inheritdoc />
    public async Task ClearSessionAsync(CancellationToken cancellationToken = default)
    {
        _currentSession = null;
        _tenantStateProvider.SetTenant(TenantState.Default);
        OnSessionChanged?.Invoke();
        if (_storage is not null)
        {
            try { await _storage.RemoveItemAsync(StorageKey, cancellationToken); }
            catch { /* A sessão em memória permanece encerrada se o navegador desconectar. */ }
        }
    }

    private void ApplySession(AuthenticatedTenantUserDto session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _currentSession = session;

        var primary = session.Branding?.PrimaryColor ?? TenantBranding.Default.PrimaryColor;
        var secondary = session.Branding?.SecondaryColor ?? TenantBranding.Default.SecondaryColor;
        var companyName = session.Branding?.AgencyName ?? session.Subdomain;

        var branding = new TenantBranding(
            PrimaryColor: primary,
            SecondaryColor: secondary,
            AccentColor: TenantBranding.Default.AccentColor,
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
}
