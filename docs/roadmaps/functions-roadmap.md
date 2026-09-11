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

### Subfase 1.3: Matriz de Perfis e Permissões (RBAC Granular)
* **1.3.1 (TDD - Red):** Testes para o avaliador de permissões (`IPermissionEvaluator`) cobrindo toda a matriz[cite: 6]:
  * *Owner / Administrador:* Acesso irrestrito ao Tenant[cite: 6].
  * *Líder de Squad:* Gestão restrita aos clientes e membros do squad[cite: 6].
  * *Gestor de Mídia:* Permissão de edição e ajustes até teto configurado[cite: 6].
  * *Analista de Tráfego / Visualizador:* Leitura estrita[cite: 6].
  * *Client Guest (Cliente Final):* Visualização blindada apenas do seu Workspace, sem acesso a margens, markups ou regras internas[cite: 6].
* **1.3.2 (TDD - Green):** Implementar os policies de autorização no ASP.NET Core e filtros do MediatR.
* **1.3.3:** Gravação de auditoria imutável em `TenantAuditLog` a cada alteração de permissão operacional[cite: 6].
* **1.3.4 (Documentação Viva):** Publicar matriz RBAC atualizada em `docs/modules/tenants-rbac.md`[cite: 6].

### Subfase 1.4: Módulo White-Label e CNAME Dinâmico
* **1.4.1 (TDD - Red):** Testes unitários para `TenantBranding` validando formato de arquivos de imagem (logo claro/escuro, favicon) e códigos hexadecimais válidos para cores primárias/secundárias[cite: 6].
* **1.4.2 (TDD - Green):** Implementar handlers de atualização de marca e injeção dinâmica de CSS no layout raiz do Blazor Server.
* **1.4.3:** Implementar resolução dinâmica de requisições por subdomínio CNAME próprio (ex.: `relatorios.agencia.com.br`)[cite: 6].

---

## Fase 2: Hub de Integrações OAuth2 & Ingestão de Dados

Módulo responsável pela comunicação com os gerenciadores de anúncios externos[cite: 3, 4].

### Subfase 2.1: Provedores de Autenticação OAuth2 e Token Vault
* **2.1.1 (TDD - Red):** Testes para adaptadores OAuth2 validando troca de código por tokens e renovação preventiva de tokens de acesso[cite: 5]:
  * `MetaAdsOAuthAdapter` (Meta Graph API)[cite: 3, 4]
  * `GoogleAdsOAuthAdapter` (Google Ads API & Developer Token)[cite: 4, 5]
  * `BingAdsOAuthAdapter` (Microsoft Advertising Platform)[cite: 4, 5]
  * `TikTokAdsOAuthAdapter` (TikTok Marketing API)[cite: 4, 5]
* **2.1.2 (TDD - Green):** Implementar adaptadores encapsulados atrás da interface `IAdNetworkAuthService`.
* **2.1.3:** Criptografar tokens de acesso e refresh tokens em repouso no banco do tenant usando AES-256.
* **2.1.4 (Documentação Viva):** Documentar escopos necessários e URLs de callback em `docs/modules/integrations-oauth.md`.

### Subfase 2.2: Sincronização Estrutural de Campanhas
* **2.2.1 (TDD - Red):** Testes unitários para mapeamento unificado: converter a estrutura nativa de cada rede para o modelo universal (Conta &rarr; Campanha &rarr; Grupo/Conjunto &rarr; Anúncio/Criativo)[cite: 6].
* **2.2.2 (TDD - Green):** Implementar rotina de sincronização paginada com tratamento de backoff exponencial para limites de requisição[cite: 5].
* **2.2.3:** Emissão do evento in-memory `CampaignHierarchySyncedEvent` após conclusão da sincronização.

### Subfase 2.3: Pipeline de Ingestão de Métricas Diárias e Horárias
* **2.3.1 (TDD - Red):** Testes de idempotência: garantir que re-execuções da sincronização de uma mesma data não dupliquem registros de métricas (Spend, Impressions, Clicks, Conversions)[cite: 4].
* **2.3.2 (TDD - Green):** Implementar repositório `ICampaignMetricsRepository` com inserção/atualização atômica em lote.
* **2.3.3 (Frontend Blazor):** Painel de status `ConnectionStatusList.razor` sinalizando tokens válidos e botão para renovação imediata de credenciais[cite: 5].

---

## Fase 3: Métricas Cross-Network, Atribuição & Dashboard Unificado

Centralização dos dados analíticos e inteligência de performance[cite: 3, 4].

### Subfase 3.1: Normalização Cambial e Taxonomia Automatizada
* **3.1.1 (TDD - Red):** Testes unitários para o serviço `ICurrencyConverter`: converter custos em USD, EUR e BRL pela cotação do dia para visualização padronizada[cite: 4].
* **3.1.2 (TDD - Green):** Implementar serviço de conversão cambial com suporte a cache local.
* **3.1.3:** Implementar classificador de taxonomia por tags que mapeia nomenclaturas (ex.: `[TOF]` ou `Topo` &rarr; Prospecção; `[BOF]` ou `Remarketing` &rarr; Fundo de Funil)[cite: 3, 4].
* **3.1.4 (Documentação Viva):** Registrar convenções de taxonomia em `docs/modules/analytics-taxonomy.md`[cite: 4].

### Subfase 3.2: Atribuição Multicanal, MER e Blended Metrics
* **3.2.1 (TDD - Red):** Testes unitários para as fórmulas financeiras agregadas[cite: 3, 4]:
  * **MER (*Marketing Efficiency Ratio*):** $\text{Receita Total} \div \text{Gasto Total Consolidado}$[cite: 3, 4]
  * **Blended ROAS:** $\text{Receita de Conversões} \div \sum \text{Investimento Total}$[cite: 3, 4]
  * **Blended CAC:** $\sum \text{Investimento Total} \div \sum \text{Novos Clientes}$[cite: 3, 4]
* **3.2.2 (TDD - Green):** Implementar o calculador `BlendedMetricsCalculator` retornando `Result<BlendedMetricsDto>`.
* **3.2.3:** Implementar modelos de atribuição (Primeiro Clique, Último Clique e Linear) para ilustrar como o tráfego assistido converte entre canais[cite: 3, 4].

### Subfase 3.3: Dashboard Unificado no Blazor Server
* **3.3.1 (TDD - bUnit):** Testes de renderização para cartões de métricas principais (Spend, CPC, CPM, CTR, CPA, ROAS) comparando com período anterior[cite: 3, 4].
* **3.3.2:** Desenvolver painel executivo com gráficos interativos e filtros globais (Workspace, Canal, Período, Dispositivo)[cite: 4].
* **3.3.3:** Exportação rápida de visões em formatos CSV e imagens de alta resolução.

---

## Fase 4: Automações Cross-Platform, Lances & Travas de Segurança

Motor de processamento programado de ações baseadas em regras de negócio[cite: 3, 4].

### Subfase 4.1: Construtor de Regras Cross-Platform (DSL / Condições If-Then)
* **4.1.1 (TDD - Red):** Testes para avaliador de condições: validar gatilhos combinados (ex.: *Se CPA do TikTok Ads > R$ 50 nas últimas 48h E Google Ads ROAS > 4.5 &rarr; Reduzir TikTok em 20% e alocar saldo no Google*)[cite: 3, 4].
* **4.1.2 (TDD - Green):** Implementar árvore de predicados e motor de avaliação de regras desacoplado de dependências de rede.
* **4.1.3:** Implementar disparador de comandos de mutação (ajustar verba, pausar anúncio) que emite solicitações ao módulo `Integrations`.
* **4.1.4 (Documentação Viva):** Criar `docs/modules/automations-rules.md` com exemplos práticos de esquemas JSON para configuração de regras[cite: 4].

### Subfase 4.2: Travas de Segurança (Overspending & Detector 404/500)
* **4.2.1 (TDD - Red):** Testes unitários para `OverspendingGuard`: disparar pausa imediata e alarme se o gasto diário superar 120% do orçamento configurado[cite: 3, 4].
* **4.2.2 (TDD - Green):** Implementar serviço de monitoramento contínuo com envio de notificações push/webhook para Slack, WhatsApp e E-mail[cite: 4].
* **4.2.3:** Implementar o serviço `LandingPageHealthChecker` que faz requisições periódicas (HTTP `HEAD`) nas URLs de destino dos anúncios; pausar automaticamente anúncios cujo link retorne erro HTTP 4xx ou 5xx[cite: 3, 4].

### Subfase 4.3: Gestão Dinâmica de Budget & Previsão de Fim de Mês (Pacing)
* **4.3.1 (TDD - Red):** Testes unitários para cálculo de projeção de consumo de verba (gasto projetado vs. contratado)[cite: 3, 4].
* **4.3.2 (TDD - Green):** Implementar calculador de pacing com classificação em 3 status: *No Ritmo*, *Sobreaquecido (Over)* e *Subinvestido (Under)*[cite: 4].
* **4.3.3 (Frontend Blazor):** Componente visual `BudgetPacingBar.razor` indicando a velocidade de consumo por cliente[cite: 4].

---

## Fase 5: Operações em Massa, Biblioteca de Criativos, IA e Relatórios

Recursos avançados de produtividade e entrega de valor ao usuário final[cite: 3, 4].

### Subfase 5.1: Edição e Operações em Massa Multiplataforma
* **5.1.1 (TDD - Red):** Testes unitários para `BulkCampaignOperationCommand` validando ações em lote (ativar, pausar ou reajustar orçamento em 30 campanhas de plataformas diferentes simultaneamente)[cite: 3, 4].
* **5.1.2 (TDD - Green):** Implementar orquestrador em lote com padrão de tolerância a falhas parciais (retornando lista explícita de itens alterados com sucesso e itens que falharam).
* **5.1.3 (Frontend Blazor):** Tabela matricial de edição rápida permitindo modificações de orçamentos com pré-visualização de impacto antes da confirmação[cite: 4].

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