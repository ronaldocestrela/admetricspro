# Especificação de Módulo: Visão Geral do Tenant (`TenantDashboardPage`)

Este documento especifica a página de **Visão Geral Operacional** (`/dashboard`) do inquilino no `WebApp`, desenvolvida na **Subfase 3.2** do roadmap e regida pelos princípios arquiteturais inegociáveis do [AGENTS.md](file:///home/rony/LPR/AdMetricsPro/AGENTS.md).

---

## 1. Visão Geral e Princípios Arquiteturais

A tela de Visão Geral (`/dashboard`) é a primeira interface apresentada ao gestor de tráfego e à equipe da agência após o onboarding e no fluxo diário pós-autenticação. Foi desenhada para eliminar o erro 404 e fornecer uma experiência premium, informativa e orientadora:

1. **Frontend Estritamente de Apresentação (Zero Acesso a Banco):** A página consome exclusivamente dados fornecidos pelo estado do circuito Blazor Server (`ITenantSessionStateProvider`, `ITenantStateProvider`). É expressamente proibida a injeção de `DbContext` ou execução de consultas SQL no frontend.
2. **Recepção Humanizada e Contextual:** O cabeçalho identifica nominalmente o gestor autenticado através do payload da sessão (`AuthenticatedTenantUserDto.FullName`), recorrendo a fallback gracioso (*"Gestor"*) quando não autenticado.
3. **Empty State Elegante & FTUX (First-Time User Experience):** Em contas recém-provisionadas sem conexões de anúncios ativas, a interface apresenta métricas zeradas sem sensação de sistema vazio ou quebrado, acompanhadas de uma trilha de primeiros passos orientativa.
4. **Visibilidade Imediata das 4 Redes de Anúncios:** Monitoramento dos canais centrais suportados pelo AdMetricsPro — **Meta Ads, Google Ads, TikTok Ads e Bing Ads** — com status de conexão e atalhos rápidos para autorização OAuth2.
5. **Adesão ao Design System White-Label:** Uso exclusivo de variáveis CSS globais (`--tenant-primary`, `--tenant-accent`, `--tenant-secondary`), dark theme nativo, glassmorphism e microinterações fluidas.

---

## 2. Estrutura de Componentes e Rotas

| Componente | Rota | Layout | Responsabilidade |
| :--- | :--- | :--- | :--- |
| `TenantDashboardPage` | `/dashboard` | `TenantMainLayout` | Página principal de visualização consolidada de tráfego, KPIs consolidados, atalhos operacionais e onboarding FTUX. |

### Arquivos Relacionados
- `src/Frontend/WebApp/Components/Pages/TenantDashboardPage.razor`: Estrutura semântica e lógica reativa do componente.
- `src/Frontend/WebApp/Components/Pages/TenantDashboardPage.razor.css`: Folha de estilos isolada (scoped) com tema escuro, glassmorphism e variáveis CSS de white-label.
- `tests/UnitTests/Frontend/Components/Pages/TenantDashboardPageTests.cs`: Suíte de testes unitários automatizados com bUnit.

---

## 3. Seções e Especificações Visuais

### 3.1 Cabeçalho Operacional
- **Título de Boas-Vindas:** `"Olá, [Nome do Gestor]! Bem-vindo à sua central de tráfego."`
  - Consome `TenantSessionStateProvider.CurrentSession?.FullName`.
  - Fallback: `"Olá, Gestor! Bem-vindo à sua central de tráfego."`.
- **Subtítulo:** Identificação da agência ativa (`TenantStateProvider.CurrentTenant.Name`).
- **Ações Rápidas (CTA):**
  - Botão Primário: `"Conectar Contas"` (direciona para `/integrations`).
  - Botão Secundário: `"Novo Workspace"` (direciona para `/workspaces`).

### 3.2 Cards de Métricas Consolidadas (KPIs)
Grid responsivo com 6 indicadores de performance de tráfego em estado inicial zerado:

| Indicador | Valor Inicial | Subtexto / Contexto | Ícone |
| :--- | :--- | :--- | :--- |
| **Investimento Total** | `R$ 0,00` | 0,0% vs período anterior | Moeda / Cifrão |
| **Receita Atribuída** | `R$ 0,00` | Faturamento bruto via Pixel/CAPI | Gráfico ascendente |
| **ROAS Consolidado** | `0,00x` | Retorno unificado sobre ad spend | Alvo / Precisão |
| **MER Médio** | `0,00%` | Marketing Efficiency Ratio global | Percentual |
| **Cliques & Impressões** | `0 clics / 0 imp.` | Alcance unificado cross-network | Olho / Impressão |
| **CPA Médio** | `R$ 0,00` | Custo por aquisição consolidado | Carrinho de compras |

### 3.3 Checklist Interativo de Primeiro Acesso (FTUX — `AgencyFtuxChecklist`)
Na parte superior do dashboard, é renderizado o componente interativo `<AgencyFtuxChecklist />` (especificado detalhadamente em [tenant-ftux-wizard.md](file:///home/rony/LPR/AdMetricsPro/docs/modules/tenant-ftux-wizard.md)), conduzindo o gestor através de 4 passos essenciais com barra de progresso (0% a 100%):
1. **Passo 1 — Banco e Ambiente Provisionados (25%):** Conclusão automática com a criação do banco dedicado.
2. **Passo 2 — Cadastrar 1º Cliente / Workspace (25%):** Abertura do modal rápido `WorkspaceQuickModal`.
3. **Passo 3 — Conectar 1ª Conta de Anúncios (25%):** Abertura do modal rápido `AdConnectionQuickModal` com modo demonstrativo imediato ("Carregar Dados Demonstrativos").
4. **Passo 4 — Convidar Equipe ou Criar Squad (25%):** Abertura do modal rápido `TeamQuickModal` (convite por e-mail com RBAC ou criação de squad).

Ao atingir 100%, o componente exibe um card de comemoração com opção de ocultar o checklist.

### 3.4 Monitoramento das Redes Suportadas
Cards de status das 4 redes de anúncios nativas:
- **Meta Ads:** Facebook & Instagram.
- **Google Ads:** Search, YouTube & Display.
- **TikTok Ads:** TikTok For Business.
- **Bing Ads:** Microsoft Advertising.
- **Status Inicial:** Badge `Desconectado` com indicador visual e botão `"Conectar"`.

---

## 4. Reatividade do Circuito Blazor Server

O componente assina os eventos de notificação do circuito Blazor no ciclo de vida:
```csharp
protected override void OnInitialized()
{
    TenantSessionStateProvider.OnSessionChanged += HandleStateChanged;
    TenantStateProvider.OnTenantChanged += HandleStateChanged;
}

public void Dispose()
{
    TenantSessionStateProvider.OnSessionChanged -= HandleStateChanged;
    TenantStateProvider.OnTenantChanged -= HandleStateChanged;
}
```
Isso assegura que qualquer alteração de tenant ou autenticação em segundo plano atualize instantaneamente a saudação e o branding sem recarregar a página inteira.

---

## 5. Cobertura de Testes Automatizados (bUnit)

A classe `TenantDashboardPageTests` valida 100% dos fluxos e comportamentos da página:
1. `TenantDashboardPage_WhenUserAuthenticated_ShouldRenderPersonalizedWelcomeHeader`: Renderização da saudação personalizada com o nome completo do gestor.
2. `TenantDashboardPage_WhenNoUserInSession_ShouldRenderFallbackWelcomeHeader`: Fallback para "Gestor" na ausência de sessão ativa.
3. `TenantDashboardPage_ShouldRenderAgencyNameInSubtitle`: Presença do nome corporativo da agência no subtítulo.
4. `TenantDashboardPage_ShouldRenderZeroedKpiMetricCards`: Validação dos 6 cards de KPI e seus respectivos valores zerados.
5. `TenantDashboardPage_ShouldRenderEmptyStateWithActionShortcuts`: Validação do banner FTUX e dos links para `/integrations`, `/workspaces` e `/squads`.
6. `TenantDashboardPage_ShouldRenderAllFourSupportedAdPlatformsWithDisconnectedStatus`: Validação dos 4 canais mandatários em estado desconectado.
7. `TenantDashboardPage_WhenSessionStateChanges_ShouldReRenderWithNewName`: Atualização reativa de interface quando o evento `OnSessionChanged` é disparado.
