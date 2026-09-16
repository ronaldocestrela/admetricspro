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
            _ = Task.Run(async () =>
            {
                try
                {
                    await _storage.SetItemAsync(StorageKey, session);
                }
                catch
                {
                    // Tratamento gracioso em caso de restrições de JSInterop durante pré-render
                }
            });
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
    public void ClearSession()
    {
        _currentSession = null;
        _tenantStateProvider.SetTenant(TenantState.Default);

        if (_storage != null)
        {
            try
            {
                var vt = _storage.RemoveItemAsync(StorageKey);
                if (!vt.IsCompletedSuccessfully)
                {
                    vt.AsTask().GetAwaiter().GetResult();
                }
            }
            catch
            {
                // Silencioso
            }
        }

        OnSessionChanged?.Invoke();
    }

    private void ApplySession(AuthenticatedTenantUserDto session)
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
}
