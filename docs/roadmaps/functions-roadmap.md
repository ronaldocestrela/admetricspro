# Roadmap Detalhado de Implementação Funcional: Core SaaS

Este documento estabelece o planejamento técnico e funcional exaustivo para o desenvolvimento das regras de negócio e módulos operacionais da plataforma SaaS de Gestão Unificada de Tráfego Pago (integrando **Meta Ads**, **Google Ads**, **Microsoft Advertising / Bing Ads** e **TikTok Ads**)[cite: 3, 4].

Todas as entregas seguem as diretrizes inegociáveis do `AGENTS.md`: **.NET 10**, **Blazor Server**, **Monólito Modular**, **SQL Server com isolamento database-per-tenant**, **Repository Pattern**, **Result<T>**, **TDD estrito** e **Documentação Viva**.

---

## Visão Geral das Fases Funcionais

| Fase | Módulo Funcional | Limites de Contexto (Namespace) | Entregável Principal |
| :--- | :--- | :--- | :--- |
| **Fase 1** | Workspaces, Squads & RBAC Granular | `Modules/Tenants` | Estrutura hierárquica por cliente, squads, papéis funcionais e White-Label[cite: 6]. |
| **Fase 2** | Hub de Integrações OAuth2 & Ingestão | `Modules/Integrations` | Conectores oficiais (Meta, Google, Bing, TikTok), pipelines de ETL e health checks[cite: 3, 4]. |
| **Fase 3** | Métricas Cross-Network & Atribuição | `Modules/Analytics` | Dashboard consolidado, normalização cambial, cálculo de MER e Blended ROAS[cite: 3, 4]. |
| **Fase 4** | Automações Cross-Platform, Lances & Travas | `Modules/Automations` | Motor de regras condicionais (If/Then), travas de overspending e pacing[cite: 3, 4]. |
| **Fase 5** | Operações em Massa, Ativos & Relatórios | `Modules/Operations` & `Reports` | Edição em lote, Creative Hub com fadiga, Copiloto IA e relatórios White-Label[cite: 3, 4]. |

---

## Fase 1: Workspaces, Gestão de Squads, RBAC Granular e White-Label

Esta fase implementa a governança e o isolamento de dados no banco dedicado de cada inquilino (`TenantDbContext`).

### Subfase 1.1: Workspaces e Cadastro de Clientes `[CONCLUÍDA]`
* [x] **1.1.1 (TDD - Red):** Escrever testes unitários para a entidade `Workspace` validando:
  * Obrigatoriedade de CNPJ/CPF e Razão Social válidos (`WorkspaceTests.cs`).
  * Validação de teto máximo de Workspaces permitidos pela cota da assinatura do Tenant (`CreateWorkspaceCommandHandlerTests.cs`).
* [x] **1.1.2 (TDD - Green):** Implementar o agregado `Workspace` (`Workspace.cs`) com métodos de fábrica estáticos `Workspace.Create(...)`, algoritmos de documento módulo 11 e invariantes `UpdateDetails`, `Activate`, `Deactivate`.
* [x] **1.1.3:** Implementar `IWorkspaceRepository` com métodos assíncronos que recebem `CancellationToken` e mapeamento EF Core no `TenantDbContext`.
* [x] **1.1.4:** Implementar o handler `CreateWorkspaceCommand` retornando `Result<Guid>` ou falha tipada `Error.Conflict` / `Error.Validation`, além de `UpdateWorkspaceCommand` e `ToggleWorkspaceStatusCommand`.
* [x] **1.1.5 (Documentação Viva & Frontend):** 
  * Criar e consolidar `docs/modules/tenants-workspaces.md` documentando schema de entrada, payload JSON, retornos com códigos semânticos e catálogo de erros.
  * Implementar o cliente HTTP tipado `IWorkspaceClientService` / `WorkspaceClientService` e a tela completa de gestão `WorkspacesPage.razor` (`/workspaces`) com suíte bUnit em `WorkspacesPageTests.cs`.

### Subfase 1.2: Gestão de Squads (Times) e Isolamento de Carteira `[CONCLUÍDA]`
* [x] **1.2.1 (TDD - Red):** Testes unitários para a entidade `Squad` validando:
  * Associação de múltiplos membros (gestores, analistas) (`SquadTests.cs`).
  * Atribuição exclusiva ou compartilhada de Workspaces ao time (`SquadTests.cs`, `AssignSquadWorkspaceCommandHandlerTests.cs`).
  * Garantia de que um operador de um squad específico não receba dados de clientes de outro squad (`UserPortfolioServiceTests.cs`, `GetUserAccessibleWorkspacesQueryHandlerTests.cs`, `ValidateUserWorkspaceAccessQueryHandlerTests.cs`).
* [x] **1.2.2 (TDD - Green):** Implementar o agregado `Squad` e os handlers `AssignSquadWorkspaceCommand`, `AddSquadMemberCommand`, remoção de membros, alternância de status e serviço de governança `UserPortfolioService`.
* [x] **1.2.3 (Frontend Blazor):** Criar o cliente HTTP tipado `ISquadClientService` / `SquadClientService`, o componente `SquadManager.razor` com seleção múltipla de clientes e membros, simulador de isolamento de carteira (Portfolio Inspector) e página `/squads` (`SquadsPage.razor`), testados via **bUnit** (`SquadManagerTests.cs`, `SquadsPageTests.cs`, `SquadClientServiceTests.cs`).
* [x] **1.2.4 (Documentação Viva):** Adicionar tags XML `<summary>` em todos os comandos, DTOs e repositórios de Squads e consolidar especificação completa em `docs/modules/tenants-squads.md`.

### Subfase 1.3: Matriz de Perfis e Permissões (RBAC Granular) `[CONCLUÍDA]`
* [x] **1.3.1 (TDD - Red):** Testes para o avaliador de permissões (`IPermissionEvaluator`) cobrindo toda a matriz (`TenantPermissionEvaluatorTests.cs`):
  * *Owner / Administrador:* Acesso irrestrito ao Tenant.
  * *Líder de Squad:* Gestão restrita aos clientes e membros do squad.
  * *Gestor de Mídia:* Permissão de edição e ajustes até teto configurado.
  * *Analista de Tráfego / Visualizador:* Leitura estrita.
  * *Client Guest (Cliente Final):* Visualização blindada apenas do seu Workspace, sem acesso a margens, markups ou regras internas.
* [x] **1.3.2 (TDD - Green):** Implementar pipeline behavior de autorização no MediatR (`TenantAuthorizationBehavior`) e contratos `ICurrentUserContext` / `RequireTenantPermissionAttribute`.
* [x] **1.3.3:** Gravação de auditoria imutável em `TenantAuditLog` no banco dedicado do inquilino a cada alteração de permissão operacional (`ChangeTenantUserRoleCommandHandler`, `TenantAuditLogRepository`, `GetTenantAuditLogsQueryHandler`).
* [x] **1.3.4 (Frontend Blazor & Documentação Viva):**
  * Implementar cliente HTTP tipado `ITenantRbacClientService` / `TenantRbacClientService`.
  * Criar componentes visuais `RbacMatrixViewer.razor`, `TenantAuditLogViewer.razor`, componente de blindagem `TenantAuthorizeView.razor` e página `/rbac` (`RbacGovernancePage.razor`), testados via **bUnit** (`RbacMatrixViewerTests.cs`, `TenantAuditLogViewerTests.cs`, `TenantAuthorizeViewTests.cs`, `TenantRbacClientServiceTests.cs`).
  * Consolidar especificação completa em `docs/modules/tenants-rbac.md`.

### Subfase 1.4: Módulo White-Label e CNAME Dinâmico `[CONCLUÍDA]`
* [x] **1.4.1 (TDD - Red & Green):** Testes unitários para `TenantBranding` no domínio (`TenantBrandingTests.cs`), validando extensões aceitas de imagens (.png, .svg, .jpg, .jpeg, .webp para logos; .ico, .png, .svg para favicon), formato hexadecimal de cores (`#RRGGBB`/`#RGB`) e integridade do Value Object.
* [x] **1.4.2 (TDD - Green & Frontend):** 
  * Repositório `ITenantBrandingRepository` e handlers CQRS (`GetTenantBrandingQuery`, `UpdateTenantBrandingCommand`) no `Tenants.Application`.
  * Controlador Web API `TenantBrandingController` (`/api/v1/tenants/branding`).
  * Injeção dinâmica de CSS variables no layout raiz (`TenantMainLayout.razor`), título e favicon via `<HeadContent>`.
  * Componentes `BrandLogo.razor` (com alternância de tema e SVG fallback), página `/settings/white-label` (`WhiteLabelSettingsPage.razor`) e testes bUnit (`BrandLogoTests.cs`, `WhiteLabelSettingsPageTests.cs`).
* [x] **1.4.3 (CNAME & Resolução Dinâmica):**
  * Resolução dinâmica por Host header via `CustomDomainTenantIdentificationStrategy` com cache in-memory de alta performance (`IMemoryCache`), fonte `TenantResolutionSource.CustomDomain (4)` e integração ao `TenantIdentificationMiddleware`.
  * Catálogo MasterDb com unicidade global de domínios customizados (`GetByCustomDomainAsync`), verificação de plano (`plan.Features.HasCustomCname`) e comandos CQRS (`ConfigureTenantCustomDomainCommand`, `RemoveTenantCustomDomainCommand`, `GetTenantCustomDomainQuery`).
  * Endpoints Web API `TenantCnameController` (`/api/v1/tenants/cname`) e serviço cliente `ITenantCnameClientService` integrado ao painel com instruções de DNS CNAME (`cname.admetricspro.com`).
  * Documentação viva consolidada em `docs/modules/tenants-white-label.md` e ADR registrado em `docs/adr/0025-dynamic-cname-resolution-and-white-label-strategy.md`.

---

## Fase 2: Hub de Integrações OAuth2 & Ingestão de Dados

Módulo responsável pela comunicação com os gerenciadores de anúncios externos[cite: 3, 4].

### Subfase 2.1: Provedores de Autenticação OAuth2 e Token Vault
* [x] **2.1.1 (TDD - Red & Green):** Testes unitários para adaptadores OAuth2 validando troca de código por tokens e renovação preventiva de tokens de acesso (`MetaAdsOAuthAdapterTests.cs`, `GoogleAdsOAuthAdapterTests.cs`, `BingAdsOAuthAdapterTests.cs`, `TikTokAdsOAuthAdapterTests.cs` e `OAuthStateServiceTests.cs`):
  * `MetaAdsOAuthAdapter` (Meta Graph API v21.0 com troca automática por *long-lived token* de 60 dias)
  * `GoogleAdsOAuthAdapter` (Google Ads API com consent offline e refresh token)
  * `BingAdsOAuthAdapter` (Microsoft Advertising Platform / Entra v2.0 com escopos msads.manage)
  * `TikTokAdsOAuthAdapter` (TikTok Marketing API v1.3 com retorno de advertiser_ids)
* [x] **2.1.2 (TDD - Green & Orquestração):** Orquestrador unificado `IAdNetworkAuthService` (`AdNetworkAuthService.cs`) encapsulando os adaptadores especializados e despachando por plataforma.
* [x] **2.1.3 (Token Vault & Criptografia AES-256):**
  * Entidade `OAuthTokenVault` e repositório `IOAuthTokenVaultRepository` (`OAuthTokenVaultRepository.cs`).
  * Cifragem simétrica com AES-256-CBC e IV aleatório por registro em repouso no banco dedicado do inquilino (`TenantDbContext`).
  * Comandos e consultas CQRS (`InitiateOAuthFlowCommand`, `HandleOAuthCallbackCommand`, `RefreshExpiringTokensCommand`, `RevokeOAuthConnectionCommand`, `GetOAuthConnectionsStatusQuery`).
  * Endpoints RESTful Web API no `OAuthIntegrationsController.cs` (`/api/v1/integrations/oauth/*`) com OpenAPI + Scalar UI.
* [x] **2.1.4 (Documentação Viva & ADR):**
  * Documentação viva de escopos, payloads e URLs de callback consolidada em `docs/modules/integrations-oauth.md`.
  * ADR registrado em `docs/adr/0026-oauth2-hub-and-token-vault-encryption.md`.

### Subfase 2.2: Sincronização Estrutural de Campanhas
* [x] **2.2.1 (TDD - Red & Green):** Testes unitários para mapeamento unificado: converter a estrutura nativa de cada rede para o modelo universal (Conta &rarr; Campanha &rarr; Grupo/Conjunto &rarr; Anúncio/Criativo) (`CampaignHierarchyDomainTests.cs`, `MetaAdsHierarchyMapperTests.cs`, `GoogleAdsHierarchyMapperTests.cs`, `TikTokAdsHierarchyMapperTests.cs` e `BingAdsHierarchyMapperTests.cs`).
* [x] **2.2.2 (TDD - Green):** Rotina de sincronização paginada com tratamento de backoff exponencial com jitter para limites de requisição (`HierarchyRateLimitPolicy.cs`), adaptadores por rede com typed HttpClients (`MetaAdsHierarchySyncAdapter.cs`, `GoogleAdsHierarchySyncAdapter.cs`, `TikTokAdsHierarchySyncAdapter.cs`, `BingAdsHierarchySyncAdapter.cs`), adaptador determinístico para FTUX (`DemoHierarchySyncAdapter.cs`), despachante dinâmico (`CampaignHierarchySyncDispatcher.cs`), repositório de persistência atômica no banco do inquilino (`CampaignHierarchyRepository.cs` e `TenantDbContext`).
* [x] **2.2.3 (CQRS & Evento In-Memory):**
  * Emissão do evento de domínio in-memory `CampaignHierarchySyncedEvent` após persistência bem-sucedida.
  * Comandos e consultas CQRS (`SyncCampaignHierarchyCommand` e `GetCampaignHierarchyQuery`).
  * Endpoints RESTful Web API no `CampaignsController.cs` (`/api/v1/integrations/campaigns/*`) com documentação OpenAPI + Scalar UI.
  * Documentação viva consolidada em `docs/modules/integrations-campaign-hierarchy-sync.md` e ADR registrado em `docs/adr/0027-campaign-structural-sync-and-rate-limiting.md`.

### Subfase 2.3: Pipeline de Ingestão de Métricas Diárias e Horárias
* [x] **2.3.1 (TDD - Red & Green):** Testes de idempotência: garantir que re-execuções da sincronização de uma mesma data não dupliquem registros de métricas (`CampaignMetricDomainTests.cs` com 11 testes e `CampaignMetricsRepositoryTests.cs` com 3 testes unitários comprovando que re-execuções da mesma data preservam registros e atualizam valores via upsert atômico).
* [x] **2.3.2 (TDD - Green & CQRS):**
  * Repositório `ICampaignMetricsRepository` e implementação `CampaignMetricsRepository.cs` com upsert atômico em lote no `TenantDbContext`.
  * Configuração fluente `CampaignMetricEntityTypeConfiguration.cs` com índice único composto de idempotência (`ConnectedAdAccountId, ExternalCampaignId, ExternalAdSetId, ExternalAdId, Date, Hour, Granularity`).
  * Adaptadores analíticos com clientes HTTP resilientes (`MetaAdsMetricsSyncAdapter.cs`, `GoogleAdsMetricsSyncAdapter.cs`, `TikTokAdsMetricsSyncAdapter.cs`, `BingAdsMetricsSyncAdapter.cs`, `DemoMetricsSyncAdapter.cs`) e despachante `CampaignMetricsSyncDispatcher.cs` orquestrado com `HierarchyRateLimitPolicy`.
  * Comandos e consultas CQRS (`SyncCampaignMetricsCommand`, `GetCampaignMetricsQuery`) e evento in-memory `CampaignMetricsSyncedEvent`.
  * Endpoints RESTful Web API no `CampaignMetricsController.cs` (`/api/v1/integrations/campaigns/metrics/*`) integrados com OpenAPI e Scalar UI.
  * Documentação viva em `docs/modules/integrations-campaign-metrics-ingestion.md` e ADR registrado em `docs/adr/0028-campaign-metrics-ingestion-and-idempotency.md`.
* [x] **2.3.3 (Frontend Blazor & bUnit):**
  * Cliente HTTP fortemente tipado `OAuthIntegrationsClientService.cs` (`IOAuthIntegrationsClientService.cs`) consumindo exclusivamente a Web API (`/api/v1/integrations/oauth/*`), com zero acesso direto a banco de dados (conforme Regra 9 do `AGENTS.md`).
  * Painel de conexões `ConnectionStatusList.razor` com estilos isolados `ConnectionStatusList.razor.css`, sinalizando saúde das contas de anúncio (Ativo, Expirando, Revogado) e botão de ação para renovação imediata de credenciais.
  * Integrado à tela `WorkspacesPage.razor` com modal/seção de gerenciamento de integrações.
  * Testes de componente com **bUnit** em `ConnectionStatusListTests.cs` (4/4 testes passando com sucesso).

---

## Fase 3: Métricas Cross-Network, Atribuição & Dashboard Unificado

Centralização dos dados analíticos e inteligência de performance[cite: 3, 4].

### Subfase 3.1: Normalização Cambial e Taxonomia Automatizada
* [x] **3.1.1 (TDD - Red):** Testes unitários para o serviço `ICurrencyConverter`: converter custos em USD, EUR e BRL pela cotação do dia para visualização padronizada (`CurrencyConverterTests.cs` com 6 testes cobrindo conversão direta, triangulação, mesma moeda, validação de negativos, moedas não suportadas e conversão em lote).
* [x] **3.1.2 (TDD - Green):** Implementar serviço de conversão cambial com suporte a cache local (`CurrencyConverter.cs`, `CanonicalExchangeRateProvider.cs` com cache em dois níveis via `IMemoryCache` de 7 dias para histórico e 30 minutos para intraday).
* [x] **3.1.3:** Implementar classificador de taxonomia por tags que mapeia nomenclaturas (ex.: `[TOF]` ou `Topo` &rarr; Prospecção; `[BOF]` ou `Remarketing` &rarr; Fundo de Funil; `[RET]` &rarr; Retenção; além de tipos de público e formatos) com normalização de delimitadores (`AutomatedTaxonomyClassifier.cs` e 19 testes em `AutomatedTaxonomyClassifierTests.cs`).
* [x] **3.1.4 (Documentação Viva & API):**
  * Criação do módulo autônomo `Analytics` (`Analytics.Domain`, `Analytics.Application`, `Analytics.Infrastructure`).
  * Consultas e handlers CQRS (`ConvertCurrencyQuery`, `ConvertCurrencyBatchQuery`, `ClassifyTaxonomyQuery`, `BatchClassifyTaxonomyQuery` com testes em `AnalyticsQueriesTests.cs`).
  * Endpoints RESTful Web API no `AnalyticsController.cs` (`/api/v1/analytics/*`) com documentação OpenAPI + Scalar UI e testes em `AnalyticsControllerTests.cs`.
  * Registrar convenções de taxonomia em `docs/modules/analytics-taxonomy.md` e ADR registrado em `docs/adr/0029-currency-normalization-and-automated-taxonomy.md`.

### Subfase 3.2: Atribuição Multicanal, MER e Blended Metrics
* [x] **3.2.1 (TDD - Red):** Testes unitários para as fórmulas financeiras agregadas (`BlendedMetricsCalculatorTests.cs` com testes cobrindo MER, Blended ROAS, Blended CAC, CPA, CPC, CPM, CTR, proteção contra divisão por zero, conversão multi-moeda integrada e quebra percentual por canal).
  * **MER (*Marketing Efficiency Ratio*):** $\text{Receita Total} \div \text{Gasto Total Consolidado}$
  * **Blended ROAS:** $\text{Receita de Conversões} \div \sum \text{Investimento Total}$
  * **Blended CAC:** $\sum \text{Investimento Total} \div \sum \text{Novos Clientes}$
* [x] **3.2.2 (TDD - Green):** Implementação do calculador de alta performance `BlendedMetricsCalculator` (`IBlendedMetricsCalculator`) retornando `Result<BlendedMetricsResult>` / `Result<BlendedMetricsDto>` com normalização cambial multi-moeda e shares de investimento somando 100%.
* [x] **3.2.3 (Atribuição Multicanal & TDD):** Implementação do motor de atribuição `AttributionCalculator` (`IAttributionCalculator`) e testes em `AttributionCalculatorTests.cs` cobrindo Primeiro Clique (*First-Touch* 100%), Último Clique (*Last-Touch* 100%), Linear ($1/N$), contabilização de Conversões Assistidas (*Assisted Conversions*) e comparador side-by-side dos 3 modelos.
* [x] **3.2.4 (Documentação Viva & API):**
  * Consultas e handlers CQRS (`CalculateBlendedMetricsQuery` e `CalculateAttributionQuery` com testes em `AnalyticsBlendedAndAttributionQueriesTests.cs`).
  * Endpoints RESTful Web API no `AnalyticsController.cs` (`/api/v1/analytics/blended-metrics` e `/api/v1/analytics/attribution`) com documentação OpenAPI + Scalar UI e testes em `AnalyticsBlendedAndAttributionControllerTests.cs`.
  * Documentação viva consolidada em `docs/modules/analytics-blended-and-attribution.md` e ADR registrado em `docs/adr/0030-blended-metrics-and-multichannel-attribution.md`.

### Subfase 3.3: Dashboard Unificado no Blazor Server
* [x] **3.3.1 (TDD - bUnit):** Testes de renderização para cartões de métricas principais (Spend, CPC, CPM, CTR, CPA, ROAS) comparando com período anterior (`MetricCardsGridTests.cs`, `MetricCard.razor`, `MetricCardsGrid.razor` com semântica de polaridade invertida para CPA e CPC).
* [x] **3.3.2:** Desenvolver painel executivo com gráficos interativos e filtros globais (Workspace, Canal, Período, Dispositivo) (`DashboardFiltersBar.razor`, `PerformanceTrendChart.razor`, `PlatformShareDonutChart.razor`, `DevicePerformanceBarChart.razor`, `ExecutiveDashboardView.razor`).
* [x] **3.3.3:** Exportação rápida de visões em formatos CSV e imagens de alta resolução (`DashboardExportActions.razor`, `dashboard-export.js` e testes em `DashboardExportActionsTests.cs`).

---

## Fase 4: Automações Cross-Platform, Lances & Travas de Segurança

Motor de processamento programado de ações baseadas em regras de negócio[cite: 3, 4].

### Subfase 4.1: Construtor de Regras Cross-Platform (DSL / Condições If-Then)
* [x] **4.1.1 (TDD - Red):** Testes para avaliador de condições: validar gatilhos combinados (ex.: *Se CPA do TikTok Ads > R$ 50 nas últimas 48h E Google Ads ROAS > 4.5 &rarr; Reduzir TikTok em 20% e alocar saldo no Google*)[cite: 3, 4] (`RuleConditionEvaluatorTests.cs`).
* [x] **4.1.2 (TDD - Green):** Implementar árvore de predicados e motor de avaliação de regras desacoplado de dependências de rede (`RuleConditionEvaluator`, `IRuleCondition`, `MetricPredicate`, `RuleConditionGroup`).
* [x] **4.1.3:** Implementar disparador de comandos de mutação (ajustar verba, pausar anúncio) que emite solicitações ao módulo `Integrations` (`RuleActionDispatcher`, `PauseCampaignCommandHandler`, `PauseAdCommandHandler`, `AdjustCampaignBudgetCommandHandler`, `ReallocateBudgetCommandHandler`).
* [x] **4.1.4 (Documentação Viva):** Criar `docs/modules/automations-rules.md` com exemplos práticos de esquemas JSON para configuração de regras, endpoints da Web API em `AutomationsController.cs` e ADR registrado em `docs/adr/0032-cross-platform-rules-dsl-and-action-dispatcher.md`[cite: 4].

### Subfase 4.2: Travas de Segurança (Overspending & Detector 404/500)
* [x] **4.2.1 (TDD - Red & Green):** Testes unitários para `OverspendingGuard` (`OverspendingGuardTests.cs`): disparar pausa imediata via `PauseCampaignCommand` e alarme multi-canal se o gasto diário superar 120% do orçamento configurado.
* [x] **4.2.2 (TDD - Red & Green):** Implementar subsistema de alarmes e notificações multi-canal (`SecurityAlertDispatcher`, `ISecurityAlertNotifier`, `SecurityAlertDispatcherTests.cs`) com envio resiliente para Slack (Block Kit), WhatsApp, E-mail e Webhooks com tolerância a falhas parciais.
* [x] **4.2.3 (TDD - Red & Green):** Implementar o serviço `LandingPageHealthChecker` (`LandingPageHealthCheckerTests.cs`) que faz requisições periódicas (HTTP `HEAD` com fallback para `GET`) nas URLs de destino dos anúncios; pausar automaticamente via `PauseAdCommand` anúncios cujo link retorne erro HTTP 4xx, 5xx ou timeout.
* [x] **4.2.4 (Documentação Viva & API):** Endpoints RESTful no `SecurityGuardsController.cs` (`/api/v1/automations/guards/...`), documentação OpenAPI + Scalar UI, especificação viva em `docs/modules/automations-safety-guards.md` e ADR registrado em `docs/adr/0033-safety-guards-overspending-and-broken-links.md`.

### Subfase 4.3: Gestão Dinâmica de Budget & Previsão de Fim de Mês (Pacing)
* [x] **4.3.1 (TDD - Red):** Testes unitários para cálculo de projeção de consumo de verba (gasto projetado vs. contratado)[cite: 3, 4] (`BudgetPacingCalculatorTests.cs`).
* [x] **4.3.2 (TDD - Green):** Implementar calculador de pacing com classificação em 3 status: *No Ritmo*, *Sobreaquecido (Over)* e *Subinvestido (Under)*[cite: 4] (`BudgetPacingCalculator.cs`, `GetWorkspaceBudgetPacingQueryHandler.cs`, `BudgetPacingController.cs`).
* [x] **4.3.3 (Frontend Blazor):** Componente visual `BudgetPacingBar.razor` indicando a velocidade de consumo por cliente[cite: 4], cliente HTTP `BudgetPacingClientService.cs` e testes bUnit (`BudgetPacingBarTests.cs`, `BudgetPacingClientServiceTests.cs`).

---

## Fase 5: Operações em Massa, Biblioteca de Criativos, IA e Relatórios

Recursos avançados de produtividade e entrega de valor ao usuário final[cite: 3, 4].

### Subfase 5.1: Edição e Operações em Massa Multiplataforma
* [x] **5.1.1 (TDD - Red):** Testes unitários para `BulkCampaignOperationCommand` validando ações em lote (ativar, pausar ou reajustar orçamento em 30 campanhas de plataformas diferentes simultaneamente)[cite: 3, 4].
* [x] **5.1.2 (TDD - Green):** Implementar orquestrador em lote com padrão de tolerância a falhas parciais (retornando lista explícita de itens alterados com sucesso e itens que falharam).
* [x] **5.1.3 (Frontend Blazor):** Tabela matricial de edição rápida permitindo modificações de orçamentos com pré-visualização de impacto antes da confirmação[cite: 4].

### Subfase 5.2: Creative Hub & Detector de Fadiga de Criativos
* **5.2.1 (TDD - Red):** Testes para cálculo de fadiga de anúncio: identificar criativos cujo CTR caiu progressivamente nos últimos 7 dias acompanhado de frequência elevada[cite: 3, 4].
* **5.2.2 (TDD - Green):** Implementar agregação de métricas por ativo de mídia (imagem/vídeo) permitindo comparar a eficiência da mesma peça no Meta vs. TikTok[cite: 3, 4].
* **5.2.3:** Emissão de aviso visual de substituição sugerida na tela do gestor[cite: 4].

### Subfase 5.3: Copiloto de Otimização via IA (Auditor de Tráfego)
* **5.3.1 (TDD - Red):** Testes para detector de anomalias:
  * Identificação de sobreposição de públicos no Meta Ads[cite: 4].
  * Disputa e canibalização de termos de busca entre Google e Bing Ads[cite: 4].
* **5.3.2 (TDD - Green):** Implementar gerador de diagnóstico diário sintetizado em texto natural acompanhado de botão de *Execução em 1 Clique*[cite: 4].
* **5.3.3 (Documentação Viva):** Registrar catálogo de diagnósticos e contratos de retorno em `docs/modules/ai-copilot.md`.

### Subfase 5.4: Gerador de Relatórios Automatizados em White-Label
* **5.4.1 (TDD - Red):** Testes unitários validando templates de relatórios:
  * Renderização com logotipo, cores institucionais e dados de rodapé da agência (sem referência à plataforma)[cite: 6].
  * Suporte a links web interativos com expiração e relatórios em formato PDF[cite: 3, 4].
* **5.4.2 (TDD - Green):** Implementar agendador de disparo automático (diário, semanal, mensal) via E-mail e WhatsApp[cite: 4].
* **5.4.3 (Documentação Viva):** Criar `docs/modules/reports-generator.md` documentando parâmetros de agendamento e exemplos de relatórios gerados[cite: 4].