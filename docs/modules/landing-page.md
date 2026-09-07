# Especificação Técnica — Página Inicial & Landing Page Institucional

> **Módulo:** Frontend WebApp (Blazor Server .NET 10)  
> **Rota:** `/`  
> **Layout:** `LandingLayout.razor`  
> **Design de Referência (Stitch):** Projeto ID `12765985270394356342` (Desktop: `0f96dec6c6434bf284ad4c40b1377968`, Mobile: `cb9e0f3c1897400ab881cb029be0536c`, Logo: `9ace1fd5810841dd8be12bfa99965c23`)

---

## 1. Visão Geral e Proposta de Valor

A nova Landing Page oficial do **AdMetricsPro** atua como a porta de entrada pública do produto, apresentando a proposta de valor do SaaS de gestão unificada de tráfego pago (Meta Ads, Google Ads, TikTok Ads e Bing Ads) para agências, gestores de tráfego e e-commerces.

A experiência foi concebida sob o paradigma de **Rich Aesthetics**, utilizando paleta Dark Mode com iluminação ambiente (radial glow), cards glassmórficos com desfoque de fundo (`backdrop-filter: blur(20px)`), tipografia moderna (`Plus Jakarta Sans` e `JetBrains Mono`) e gráficos analíticos vetoriais em SVG.

---

## 2. Estrutura de Componentes

Os componentes residem em `src/Frontend/WebApp/Components/Landing/` com renderização no modo `InteractiveServer`:

| Componente | Função & Responsabilidade | Elementos Chave |
| :--- | :--- | :--- |
| `LandingNavbar.razor` | Cabeçalho fixo com navegação suave e atalhos de autenticação | Logo oficial vetorial, links de âncoras (`#recursos`, `#integracoes`, `#metricas-ia`, `#depoimentos`, `#precos`), botão `Entrar` e botão `Começar Teste Grátis`. Suporte a menu hambúrguer interativo mobile. |
| `HeroSection.razor` | Hero principal com proposta de valor e CTAs de alta conversão | Tag pulsante `Novo Release: IA Preditiva de ROAS 3.0`, headline com gradiente de texto, CTA primário de teste 14 dias, CTA de demo ao vivo e selos de confiança. |
| `LiveCockpitMockup.razor` | Simulação em tempo real do cockpit com KPIs e gráfico vetorial | Floating pill com alerta dinâmico de IA (*R$ 3.200 pausados em criativos fatigados*), 4 KPI cards (Investimento Total, Receita, ROAS 4.30x e CPA) e gráfico SVG de curvas de conversão por canal. |
| `IntegrationsSection.razor` | Fita e grid de plataformas integradas | Badges de integração nativa: Meta Ads, Google Ads, TikTok Ads, Bing Ads, Shopify e Hotmart. |
| `BentoBenefitsSection.razor` | Grade Bento destacando pilares tecnológicos | Cartões interativos: *Regras Automatizadas & Anti-Overspending*, *Detector de Fadiga de Criativos* e *Atribuição Multi-Toque com MER real*. |
| `SocialProofSection.razor` | Prova social com depoimentos e métricas validadas | Depoimentos com foto/avatar, cargo de liderança e classificação de 5 estrelas. |
| `ConversionBannerSection.razor` | Banner de fechamento e conversão instantânea | Caixa com gradiente e blur, chamada direta para teste grátis sem cartão de crédito. |
| `LandingFooter.razor` | Rodapé com governança, links de conformidade e status | Links de produto, links corporativos, políticas LGPD/GDPR e indicador de sistema 100% operacional. |
| `LandingLayout.razor` | Layout mestre full-width para isolamento da Landing Page | Layout limpo sem barras laterais de aplicação interna, permitindo visão contínua do conteúdo. |

---

## 3. Tokens do Design System (`landing.css`)

```css
:root {
    --lp-bg-surface: #0f131d;
    --lp-bg-surface-low: #171b26;
    --lp-bg-surface-container: #1c1f2a;
    --lp-bg-surface-high: #262a35;
    --lp-bg-surface-highest: #313540;
    --lp-bg-surface-lowest: #0a0e18;
    
    --lp-primary: #c3c0ff;
    --lp-primary-container: #4f46e5;
    --lp-secondary: #4edea3;
    --lp-tertiary: #4cd7f6;
    
    --lp-font-sans: 'Plus Jakarta Sans', sans-serif;
    --lp-font-mono: 'JetBrains Mono', monospace;
}
```

---

## 4. Integração com o Contexto Multitenant

A página `Home.razor` avalia a sessão contextual do visitante:
* **Visitante Não Autenticado:** Renderiza a Landing Page institucional completa para conversão e descoberta de recursos.
* **Usuário com Tenant Contextual Ativo:** Exibe um banner discreto no topo identificando o Workspace ativo com link direto (`Acessar Painel do Workspace &rarr;`) para navegação instantânea.

---

## 5. Estratégia de Testes (TDD com bUnit)

Suíte localizada em:
- `tests/UnitTests/Frontend/Components/Landing/LandingPageTests.cs`:
  1. `LandingNavbar_ShouldRenderBrandLinksAndCallToActions`: Valida marca, links e CTAs.
  2. `LandingNavbar_WhenMobileMenuButtonClicked_ShouldToggleDropdown`: Valida abertura e fechamento interativo do menu mobile.
  3. `HeroSection_ShouldRenderHeadlineAnnouncementAndTrustBadges`: Valida selos de confiança, release e headline.
  4. `LiveCockpitMockup_ShouldRenderFloatingAiAlertAndEssentialKpis`: Valida alerta da IA e os 4 KPIs.
  5. `IntegrationsSection_ShouldRenderSupportedMediaPlatforms`: Valida presença dos conectores de mídia.
  6. `BentoBenefitsSection_ShouldRenderThreeValuePillars`: Valida os 3 pilares tecnológicos.
  7. `SocialProofSection_ShouldRenderCustomerTestimonialsWithRatings`: Valida depoimentos e 5 estrelas.
  8. `HomePage_ShouldRenderFullLandingPageStructure`: Valida a composição completa na rota `/`.
- `tests/UnitTests/Frontend/Components/Layout/LandingLayoutTests.cs`:
  1. `LandingLayout_ShouldRenderLandingShellWithBodyContent`: Valida a renderização correta do container raiz `.landing-shell` com o conteúdo dinâmico do `Body`.
  2. `LandingLayout_ShouldContainBlazorErrorUiContainer`: Valida a presença da estrutura `#blazor-error-ui` para tratamento resiliente de erros pelo Blazor Server.

---

## 6. Governança de UI e Tratamento de Erros

O container de aviso de erros do Blazor Server (`#blazor-error-ui`) é estilizado globalmente em `wwwroot/app.css` com `display: none;` por padrão, garantindo que nenhum layout (como `LandingLayout` ou `MainLayout`) exiba a barra de erro indevidamente durante a navegação normal, tornando-a visível apenas quando uma falha não tratada de runtime ocorre.
