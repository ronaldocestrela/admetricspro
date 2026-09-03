# Especificação do Frontend — Blazor Server (.NET 10) & White-Label

Este documento especifica a arquitetura da camada de apresentação Web do **AdMetricsPro**, detalhando os contêineres de estado, componentes reutilizáveis, slots de customização White-Label e ciclo de vida da sessão Blazor.

---

## 1. Estrutura do Módulo

O frontend está localizado em `src/Frontend/WebApp/`:

```text
src/Frontend/WebApp/
├── Components/
│   ├── App.razor                    # Raiz HTML, importação de CSS e HeadOutlet
│   ├── Routes.razor                 # Roteador Blazor apontando para MainLayout
│   ├── _Imports.razor               # Namespaces globais de componentes e estado
│   ├── Layout/
│   │   ├── MainLayout.razor         # Shell mestre com injeção de CSS Variables
│   │   └── ReconnectModal.razor     # Modal de reconexão SignalR
│   ├── Pages/
│   │   └── Home.razor               # Dashboard unificado de performance
│   └── Shared/
│       ├── AppHeader.razor          # Topbar com slot de logo e perfil
│       ├── AppSidebar.razor         # Navegação lateral expansível/retrátil
│       └── AppFooter.razor          # Rodapé com dados institucionais e White-Label
├── State/
│   ├── TenantBranding.cs            # Configurações de marca e gerador de variáveis CSS
│   ├── TenantState.cs               # Contexto imutável do Tenant na sessão
│   ├── ITenantStateProvider.cs      # Contrato de estado com eventos de notificação
│   └── TenantStateProvider.cs      # Implementação Scoped por circuito SignalR
├── wwwroot/
│   ├── css/
│   │   └── theme.css                # Design system corporativo e tokens
│   └── images/
│       └── admetricspro-logo.svg    # Logotipo padrão institucional
└── Program.cs                       # Pipeline ASP.NET Core e DI
```

---

## 2. Contratos de Estado: `ITenantStateProvider`

O `ITenantStateProvider` é registrado como `Scoped`, provendo isolamento por circuito SignalR (uma conexão por usuário/aba de navegação).

### 2.1 Modelos de Dados

```csharp
public record TenantBranding(
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string? LogoUrl = null,
    string? DarkLogoUrl = null,
    string? FaviconUrl = null,
    string? CompanyName = null,
    bool ShowPoweredBy = true);

public record TenantState(
    Guid TenantId,
    string Name,
    string Slug,
    string? CustomDomain,
    TenantBranding Branding);
```

### 2.2 Interface de Operação

```csharp
public interface ITenantStateProvider
{
    TenantState CurrentTenant { get; }
    bool IsInitialized { get; }
    event Action? OnTenantChanged;
    void SetTenant(TenantState state);
    Task InitializeAsync(TenantState? initialState = null, CancellationToken cancellationToken = default);
}
```

---

## 3. Customização White-Label e CSS Dinâmico

A injeção de estilo customizado ocorre em tempo real via variáveis CSS declaradas no método `TenantBranding.ToCssVariables()`:

```css
--tenant-primary: #2563eb;
--tenant-secondary: #0f172a;
--tenant-accent: #38bdf8;
```

### Slots de Customização Suportados:
1. **Logomarca Dinâmica (`LogoUrl`):** Renderizada no `AppHeader` com ajuste proporcional e fallback textual.
2. **Badge de Organização:** Identificação textual do tenant/workspace ativo na barra superior.
3. **Dados Institucionais no Rodapé:** Exibição da razão social ou nome da agência contratante.
4. **Supressão de Marca (`ShowPoweredBy = false`):** Quando desativado, remove por completo o selo *"Powered by AdMetricsPro"* do `AppFooter`.
