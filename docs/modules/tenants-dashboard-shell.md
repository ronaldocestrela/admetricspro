# Especificação de Módulo: Shell do Dashboard do Tenant (`TenantMainLayout`)

Este documento especifica a arquitetura, design system, tokens CSS e componentes que formam a casca visual (Shell) da aplicação autenticada de inquilinos (`WebApp`), conforme definido na **Subfase 3.1** do roadmap e nas diretrizes inegociáveis de [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md).

---

## 1. Visão Geral e Princípios Arquiteturais

A casca visual do inquilino fornece a interface de trabalho cotidiana para gestores de tráfego, administradores e proprietários de agências. Opera sob os seguintes fundamentos:

1. **Frontend Estritamente de Apresentação:** O layout consome dados visuais e contextuais através de provedores de estado em memória do circuito SignalR (`ITenantStateProvider`, `ITenantSessionStateProvider`). Não há conexão direta com bancos de dados ou instâncias de `DbContext`.
2. **Injeção Dinâmica de Tema (White-Label CSS):** As cores e customizações da agência são convertidas em variáveis CSS globais injetadas inline no elemento raiz (`.tenant-app-shell`), permitindo estilização imediata com suporte a modo escuro e glassmorphism.
3. **Monograma de Iniciais de Fallback:** Agências sem logotipo gráfico cadastrado recebem automaticamente um monograma estilizado com as iniciais da agência (ex.: "Vanguarda Digital" -> `VD`).
4. **Alerta de Período de Testes (Trial Banner):** Notificação transparente e não obstrutiva do período de avaliação gratuita com contagem de dias restantes e atalho para ativação definitiva.
5. **Navegação Operacional Padronizada:** Acesso rápido às 5 seções centrais da plataforma de tráfego pago.

---

## 2. Componentes Estruturais do Shell

| Componente | Arquivo | Responsabilidade |
| :--- | :--- | :--- |
| `TenantMainLayout` | `Components/Layout/TenantMainLayout.razor` | Layout mestre que encapsula a casca geral, injeção de CSS de White-Label e orquestra o cabeçalho, sidebar e body. |
| `TenantTopHeader` | `Components/Layout/TenantTopHeader.razor` | Cabeçalho superior com botão toggle móvel, logotipo/monograma da agência, widget de usuário e ação de logout. |
| `TenantSidebar` | `Components/Layout/TenantSidebar.razor` | Barra de navegação lateral fixa em desktop e gaveta retrátil com overlay em dispositivos móveis. |
| `TenantTrialBanner` | `Components/Layout/TenantTrialBanner.razor` | Banner superior informativo de teste gratuito (Trial) com CTA de conversão e opção de dispensar na sessão. |

---

## 3. Tokens de Tema e Variáveis CSS (White-Label)

O layout injeta as seguintes propriedades customizadas CSS no container raiz `.tenant-app-shell`:

```css
--tenant-primary-color: #2563EB;   /* Cor primária canônica da marca */
--tenant-primary: #2563EB;         /* Alias retrocompatível */
--tenant-secondary-color: #0F172A; /* Cor de fundo/secundária */
--tenant-secondary: #0F172A;       /* Alias retrocompatível */
--tenant-accent-color: #38BDF8;    /* Cor de realce e botões de destaque */
--tenant-accent: #38BDF8;          /* Alias retrocompatível */
```

---

## 4. Estrutura de Navegação Operacional

A barra lateral (`TenantSidebar`) organiza as rotas operacionais do inquilino:

```text
[ Central Operacional ]
 ├── Visão Geral (Dashboard)          -> /dashboard
 ├── Clientes (Workspaces)            -> /workspaces
 ├── Times (Squads)                   -> /squads
 └── Integrações de Anúncios          -> /integrations

[ Governança & Marca ]
 └── Configurações White-Label        -> /settings/white-label
```

---

## 5. Banner Informativo de Trial (`TenantTrialBanner`)

- **Texto Padrão:** `Ambiente de Testes (Trial) — 14 dias restantes.`
- **Botão de Ação (CTA):** `Ativar Plano Definitivo` (direciona para `/settings/billing`).
- **Comportamento de Fechamento:** O botão de fechar (`x`) oculta o banner na sessão ativa sem interromper a navegação.

---

## 6. Cobertura de Testes (TDD com bUnit)

Todos os componentes possuem 100% de cobertura bUnit em `tests/UnitTests/Frontend/Components/Layout/`:
- `TenantMainLayoutTests.cs`: Injeção de variáveis CSS, renderização de logo e monograma fallback, toggle móvel e reatividade a mudanças de estado.
- `TenantSidebarTests.cs`: Presença e links das 5 rotas obrigatórias, classe `.open` e acionamento de callback de fechamento.
- `TenantTrialBannerTests.cs`: Renderização de texto, botão CTA e capacidade de dispensar.
