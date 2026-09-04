# Roadmap de Implementação: Módulo Backoffice (Super Admin)

Este documento estabelece o planejamento técnico, sequencial e estruturado da implementação do **Backoffice Global** da plataforma SaaS de Gestão Unificada de Tráfego Pago, em conformidade estrita com o `AGENTS.md` (.NET 10, Blazor Server, Monólito Modular, SQL Server com isolamento de banco por tenant, Repository Pattern, `Result<T>`, TDD e documentação viva obrigatória).

---

## Visão Geral da Arquitetura do Backoffice

O Backoffice opera diretamente sobre o banco de catálogo central (`MasterDb`), sendo desacoplado dos bancos de dados individuais de cada tenant. Todas as mutações e regras de negócio seguem o padrão TDD estrito com o pattern `Result<T>`.

| Fase | Objetivo Central | Módulos & Camadas Impactadas | Entregável Principal |
| :--- | :--- | :--- | :--- |
| **Fase 1** | Fundação, Catálogo Master e Provisionamento | `BuildingBlocks`, `Master.Domain`, `Master.Infrastructure` | Engine de migração e criação dinâmica de bancos de tenant via TDD. |
| **Fase 2** | Governança e Diretório Global 360º | `Master.Application`, `WebApi`, `Blazor Server (Backoffice UI)` | Diretório de tenants com auditoria imutável e controle de ciclo de vida. |
| **Fase 3** | Gestão de Planos, Cobrança e Quotas | `Master.Application`, `Billing Subsystem`, `Blazor Server` | Motor de planos, parametrização de limites e regras de inadimplência (Dunning). |
| **Fase 4** | Impersonation Seguro ("Shadow Mode") | `BuildingBlocks.Security`, `WebApi Middlewares`, `Blazor Server` | Mecanismo de acesso técnico do Super Admin com trilha de auditoria blindada. |
| **Fase 5** | Monitor de Saúde das APIs e Feature Flags | `Master.Infrastructure`, `Integrations Hub`, `Blazor Server` | Dashboard de consumo de quotas (Meta, Google, Bing, TikTok) e Kill Switches. |
| **Fase 6** | Hardening, Homologação e Scalar/OpenAPI | `WebApi`, `Tests (Integration/Acceptance)`, `/docs` | Contratos de API expostos no Scalar, ADRs e cobertura E2E. |

---

## Fase 1: Fundação do MasterDb, Isolamento e Provisionamento Automático

Nesta fase inicial, estabelece-se a infraestrutura de dados para o catálogo central de tenants e o mecanismo de criação de bancos dedicados sob demanda.

### Subfase 1.1: Configuração do MasterDbContext e Migrações Globais
* **1.1.1 (TDD - Red):** Criar testes unitários para a entidade `Tenant` e para o agregador `MasterDbContext` validando constraints únicas de CNPJ, Subdomínio e Status.
* **1.1.2 (TDD - Green):** Implementar as entidades de domínio no namespace `Master.Domain.Tenants` com construtores protegidos, métodos de fábrica estáticos (`Tenant.Create(...)`) e encapsulamento estrito.
* **1.1.3:** Configurar mapeamentos via `IEntityTypeConfiguration<Tenant>` no EF Core 10 usando SQL Server.
* **1.1.4:** Implementar a execução de migração automática no host da API para o `MasterDb` através do hook de inicialização `Database.MigrateAsync()`.
* **1.1.5 (Documentação Viva):** Criar `docs/modules/backoffice-master-db.md` detalhando o schema e registrar o ADR `docs/adr/0002-database-per-tenant-strategy.md`.

### Subfase 1.2: Engine de Provisionamento Dinâmico de Bancos de Tenant
* [x] **1.2.1 (TDD - Red):** Escrever testes de integração e unitários para o serviço `ITenantProvisioningService` validando a criação de um novo banco SQL Server com nome sanitizado (ex: `Tenant_{subdomain}`) e aplicação de migrações do `TenantDbContext` via `Database.MigrateAsync()`.
* [x] **1.2.2 (TDD - Green):** Implementar o `TenantProvisioningService` utilizando comandos estruturados (`ProvisionTenantCommand`) com retorno estrito do padrão `Result<TenantId>`.
* [x] **1.2.3:** Implementar o repositório `ITenantRepository` com métodos `AddAsync`, `GetByIdAsync`, `GetBySubdomainAsync` e `CommitAsync` via `IUnitOfWork` respeitando `CancellationToken`.
* [x] **1.2.4:** Configurar a criptografia de chave simétrica (AES-256) para armazenamento seguro da Connection String de cada tenant no `MasterDb`.
* [x] **1.2.5 (Documentação Viva):** Adicionar documentação XML `<summary>` em todas as classes, métodos e contratos do provisionamento e criar a especificação completa em `docs/modules/backoffice-provisioning.md`.

---

## Fase 2: Gestão Global de Tenants e Diretório 360º

Disponibilização da visualização cadastral completa e controle do ciclo de vida dos assinantes para a equipe interna.

### Subfase 2.1: Comandos e Consultas de Tenants (Application Layer)
* [x] **2.1.1 (TDD - Red):** Testes unitários para `CreateTenantCommand`, `SuspendTenantCommand`, `ReactivateTenantCommand` e `GetTenantDetailsQuery`.
* [x] **2.1.2 (TDD - Green):** Implementar os handlers via `MediatR` no módulo `Master.Application`, retornando `Result<Unit>` ou erros tipados (ex: `Error.Conflict`, `Error.NotFound`).
* [x] **2.1.3:** Implementar o repositório de consulta `ITenantReadOnlyRepository` otimizado para consultas analíticas do diretório de assinantes.
* [x] **2.1.4 (Documentação Viva):** Atualizar `docs/modules/backoffice-tenants.md` com os payloads de entrada, saídas e possíveis erros de negócio mapeados.

### Subfase 2.2: Interface de Diretório 360º no Blazor Server
* [x] **2.2.1 (TDD - bUnit):** Escrever testes de componentes para a tabela de listagem de tenants (`TenantsGrid.razor`) com suporte a filtros por status (Ativo, Trial, Inadimplente, Suspenso, Cancelado).
* [x] **2.2.2:** Implementar a visualização da ficha completa da empresa: dados fiscais (CNPJ/Razão Social), plano contratado, total de workspaces cadastrados e volume de ad spend sincronizado.
* [x] **2.2.3:** Desenvolver diálogos de confirmação com validação dupla para ações destrutivas (suspensão forçada e desconexão de tenant).
* [x] **2.2.4 (Documentação Viva):** Criar `docs/modules/backoffice-directory-360.md` especificando os componentes Blazor, regras de validação dupla e contratos de serviço sob o padrão `Result<T>`.

---

## Fase 3: Gestão Financeira, Planos e Limites de Uso (Billing Master)

Parametrização das regras de precificação, cotas estruturais e políticas automatizadas de cobrança e inadimplência.

### Subfase 3.1: Construtor de Planos e Parametrização de Tiers
* [x] **3.1.1 (TDD - Red):** Testes unitários para a entidade `SubscriptionPlan` validando limites de assentos (seats), limites de workspaces e teto de verba gerenciada (Ad Spend Cap).
* [x] **3.1.2 (TDD - Green):** Implementar entidades e agregados de plano com suporte a flags de liberação funcional (White-Label, CNAME próprio, Copiloto de IA).
* [x] **3.1.3:** Criar repositório `IPlanRepository` e handlers `CreatePlanCommand` e `UpdatePlanCommand`.
* [x] **3.1.4:** Componente Blazor `PlanBuilder.razor` para cadastro visual de planos pela diretoria.
* [x] **3.1.5 (Documentação Viva):** Criar `docs/modules/backoffice-plans.md` documentando comandos, queries, modelo de Result<T> e endpoints OpenAPI/Scalar.

### Subfase 3.2: Régua de Inadimplência e Bloqueio Progressivo (Dunning Engine)
* [x] **3.2.1 (TDD - Red):** Testes unitários validando a política de suspensão progressiva baseada em dias de atraso (D+3: desativação de automações; D+7: bloqueio de relatórios; D+14: bloqueio de login).
* [x] **3.2.2 (TDD - Green):** Implementar o motor de dunning disparado via background service in-memory que processa o status financeiro e emite o evento `TenantGracePeriodExceededEvent`.
* [x] **3.2.3 (Documentação Viva):** Registrar o ADR `docs/adr/0011-dunning-and-tenant-lifecycle.md` e atualizar os documentos de integração financeira (`docs/modules/backoffice-dunning.md`).

---

## Fase 4: Mecanismo de Impersonation Seguro ("Shadow Mode")

Permite que técnicos de suporte acessem o ambiente do tenant para reproduzir incidentes de forma totalmente auditável.

### Subfase 4.1: Emissão e Validação de Tokens de Impersonation
* [x] **4.1.1 (TDD - Red):** Testes unitários para o `ImpersonateTenantCommand` exigindo justificativa obrigatória e número de ticket do suporte.
* [x] **4.1.2 (TDD - Green):** Implementar o gerador de token JWT contextual contendo claims especiais: `IsImpersonated=true`, `OriginalSuperAdminId` e `TenantId` de destino.
* [x] **4.1.3:** Inclusão da política de segurança que oculta dados bancários e dados de faturamento durante o modo impersonation.

### Subfase 4.2: Auditoria Master e Sinalização Visual
* [x] **4.2.1 (TDD - Red):** Testes de integração validando que toda operação realizada no modo impersonation recebe a tag `performed_by_superadmin` na tabela de auditoria global imutável.
* [x] **4.2.2:** Componente Blazor `ImpersonationBanner.razor` exibindo tarja de aviso destacada no topo da interface durante toda a sessão de impersonação com botão de encerramento imediato.
* [x] **4.2.3 (Documentação Viva):** Escrever especificações completas em `docs/modules/backoffice-impersonation.md`.

---

## Fase 5: Hub de Monitoramento de APIs e Feature Flags

Controle centralizado da saúde das integrações com Meta, Google, Bing e TikTok Ads, cotas e liberação de recursos.

### Subfase 5.1: Monitor de Rate Limits e Alertas Preventivos
* [x] **5.1.1 (TDD - Red):** Testes unitários para o agregador `ApiQuotaTracker` validando emissão de alertas quando o consumo atinge 80% do teto.
* [x] **5.1.2 (TDD - Green):** Implementar rastreamento em memória/persistência de consumo para Meta Graph API, Google Ads API, TikTok Marketing API e Bing Ads API.
* [x] **5.1.3:** Painel em Blazor `ApiHealthDashboard.razor` com indicadores visuais de tokens vencidos ou desconectados nos tenants.
* [x] **5.1.4 (Documentação Viva):** Especificações em `docs/modules/backoffice-api-health.md` e ADR `docs/adr/0014-api-quota-monitoring-and-health-tracking.md`.

### Subfase 5.2: Sistema de Feature Flags e Kill Switches Operacionais
* [x] **5.2.1 (TDD - Red):** Testes unitários para o serviço `IFeatureFlagService` com suporte a rollout percentual determinístico (SHA-256) e liberação por lista de tenants (Allowlist).
* [x] **5.2.2 (TDD - Green):** Implementação de Kill Switch global e por rede (Meta, Google, TikTok, Bing) congelando automações cross-network com auditoria imutável via `IMasterAuditService` e migração EF Core no `MasterDb`.
* [x] **5.2.3 (Documentação Viva & Frontend):** Dashboard operacional em Blazor Server (`FeatureFlagsDashboard.razor`), endpoints REST documentados com OpenAPI/Scalar em `docs/modules/backoffice-feature-flags.md` e ADR `docs/adr/0015-feature-flags-and-operational-kill-switches.md`.

---

## Fase 6: Homologação, Contratos OpenAPI com Scalar e Fechamento

Consolidação da suíte de testes de ponta a ponta, documentação viva interativa e validação dos critérios de aceite.

### Subfase 6.1: Documentação OpenAPI e Scalar UI
* [x] **6.1.1:** Configurar a exposição interativa do Scalar em `/scalar/v1` com autenticação corporativa habilitada.
* [x] **6.1.2:** Validar se todos os endpoints administrativos contêm anotações semânticas (`[EndpointSummary]`, `[ProducesResponseType]`) e exemplos estruturados do retorno `Result<T>`.

### Subfase 6.2: Validação de Conformidade com o AGENTS.md
* [x] **6.2.1:** Auditoria estrita da presença da tag XML `<summary>` em todas as classes, interfaces e records implementados no Backoffice via testes automatizados `XmlDocumentationComplianceTests` e `FrontendDocumentationComplianceTests`.
* [x] **6.2.2:** Execução completa da suíte de testes (Unitários, Integração e Aceitação) garantindo 100% de sucesso sem testes pulados ou flaky.
* [x] **6.2.3:** Verificação dos registros de ADRs atualizados na pasta `docs/adr/`, introdução do ADR `0017-compliance-architecture-and-quality-gates.md` e índice central em `docs/adr/README.md`.

---

## Fase 7: Autenticação com ASP.NET Core Identity & Backoffice Dedicado (BackofficeApp)

Isolamento do console Backoffice como aplicação Blazor Server independente (`src/Frontend/BackofficeApp`) e segurança corporativa com ASP.NET Core Identity no `MasterDb`.

### Subfase 7.1: Identity no MasterDb & Seed via .env (Backend)
* [x] **7.1.1 (TDD - Red):** Testes unitários para `AuthenticateBackofficeUserCommand`, validador e `MasterIdentitySeederTests`.
* [x] **7.1.2 (TDD - Green):** Implementação de `MasterUser`, `MasterRole`, `MasterBackofficeAuthService` e migração EF Core `Add_MasterIdentityAndUsers` no `MasterDbContext`.
* [x] **7.1.3:** Mecanismo de seed idempotente a partir das variáveis `SuperAdmin__*` do `.env` e integração com `ApplyMasterDatabaseMigrationsAsync`.

### Subfase 7.2: Aplicação Dedicada BackofficeApp & Telas Protegidas (Frontend)
* [x] **7.2.1:** Criação da aplicação autônoma `src/Frontend/BackofficeApp` em porta própria (HTTPS 7002 / HTTP 5002) com layout executivo dark mode.
* [x] **7.2.2:** Proteção global de rotas com `<AuthorizeRouteView>`, tela de login (`/login`), tela de acesso negado (`/access-denied`) e atributos `[Authorize(Roles = "SuperAdmin")]`.
* [x] **7.2.3 (TDD - bUnit):** Testes de componentes bUnit para `LoginPage` e `UserProfileBadge` em `tests/UnitTests/Frontend`.
* [x] **7.2.4 (Documentação Viva):** Especificação completa em `docs/modules/backoffice-authentication.md` e registro do ADR `docs/adr/0019-backoffice-identity-framework-and-authentication.md`.