# Architecture Decision Records (ADRs) — AdMetricsPro

Este repositório registra todas as decisões arquiteturais fundamentais adotadas no desenvolvimento do **AdMetricsPro**, estruturadas no formato padrão Nygard (Status, Contexto, Decisão e Consequências).

---

## Catálogo de Decisões Arquiteturais

| ADR | Título | Status | Data de Registro |
| :--- | :--- | :---: | :---: |
| [0001](file:///home/rony/LPR/AdMetricsPro/docs/adr/0001-modular-monolith-net10.md) | Arquitetura de Monólito Modular com .NET 10 | Aceito | 2026-09-03 |
| [0002](file:///home/rony/LPR/AdMetricsPro/docs/adr/0002-database-per-tenant-strategy.md) | Estratégia de Isolamento de Dados: Database-per-Tenant | Aceito | 2026-09-03 |
| [0003](file:///home/rony/LPR/AdMetricsPro/docs/adr/0003-tenant-connection-string-encryption.md) | Criptografia Forte de Strings de Conexão no MasterDb | Aceito | 2026-09-03 |
| [0004](file:///home/rony/LPR/AdMetricsPro/docs/adr/0004-result-pattern-and-typed-errors.md) | Padrão Result&lt;T&gt; e Erros Tipados para Controle de Fluxo | Aceito | 2026-09-03 |
| [0005](file:///home/rony/LPR/AdMetricsPro/docs/adr/0005-ddd-base-abstractions-and-persistence-contracts.md) | Abstrações DDD Base e Contratos de Persistência no BuildingBlocks | Aceito | 2026-09-03 |
| [0006](file:///home/rony/LPR/AdMetricsPro/docs/adr/0006-dynamic-tenant-resolution-pipeline.md) | Pipeline Dinâmico de Resolução de Tenant (Header, Subdomínio e JWT) | Aceito | 2026-09-03 |
| [0007](file:///home/rony/LPR/AdMetricsPro/docs/adr/0007-tenant-connection-resolver-and-dynamic-dbcontext.md) | Resolução de Conexão com Cache Seguro e TenantDbContext Dinâmico | Aceito | 2026-09-03 |
| [0008](file:///home/rony/LPR/AdMetricsPro/docs/adr/0008-automatic-migrations-pipeline-and-tenant-provisioning.md) | Pipeline de Migrações Automáticas e Provisionamento de Tenants | Aceito | 2026-09-03 |
| [0009](file:///home/rony/LPR/AdMetricsPro/docs/adr/0009-in-memory-messaging-and-validation-pipeline.md) | Comunicação Inter-Módulos In-Memory e Pipeline de Validação MediatR | Aceito | 2026-09-03 |
| [0010](file:///home/rony/LPR/AdMetricsPro/docs/adr/0010-blazor-server-frontend-and-tenant-state.md) | Frontend em Blazor Server Interativo e Gerenciamento de Estado White-Label | Aceito | 2026-09-03 |
| [0011](file:///home/rony/LPR/AdMetricsPro/docs/adr/0011-dunning-and-tenant-lifecycle.md) | Ciclo de Vida do Tenant e Régua de Cobrança / Inadimplência (Dunning) | Aceito | 2026-09-03 |
| [0012](file:///home/rony/LPR/AdMetricsPro/docs/adr/0012-impersonation-token-and-security-policy.md) | Tokens de Impersonação Temporários e Políticas de Segurança (Shadow Mode) | Aceito | 2026-09-03 |
| [0013](file:///home/rony/LPR/AdMetricsPro/docs/adr/0013-immutable-master-audit-and-visual-impersonation.md) | Auditoria Central Imutável e Sinalização Visual de Impersonação | Aceito | 2026-09-03 |
| [0014](file:///home/rony/LPR/AdMetricsPro/docs/adr/0014-api-quota-monitoring-and-health-tracking.md) | Monitoramento de Cotas, Rate Limits e Saúde de APIs de Mídia | Aceito | 2026-09-03 |
| [0015](file:///home/rony/LPR/AdMetricsPro/docs/adr/0015-feature-flags-and-operational-kill-switches.md) | Sistema de Feature Flags Determinístico e Kill Switches Operacionais | Aceito | 2026-09-03 |
| [0016](file:///home/rony/LPR/AdMetricsPro/docs/adr/0016-openapi-and-scalar-corporate-documentation.md) | Documentação de Contratos OpenAPI v1 e Interface Interativa Scalar UI | Aceito | 2026-09-03 |
| [0017](file:///home/rony/LPR/AdMetricsPro/docs/adr/0017-compliance-architecture-and-quality-gates.md) | Testes Automatizados de Conformidade Arquitetural e Guardrails do AGENTS.md | Aceito | 2026-09-03 |
| [0018](file:///home/rony/LPR/AdMetricsPro/docs/adr/0018-dotenv-configuration-and-secrets-management.md) | Gestão de Segredos e Configurações Sensíveis via Arquivo .env | Aceito | 2026-09-03 |
| [0019](file:///home/rony/LPR/AdMetricsPro/docs/adr/0019-backoffice-identity-framework-and-authentication.md) | Isolamento do Backoffice como Aplicação Dedicada e Identity Framework no MasterDb | Aceito | 2026-09-04 |
| [0020](file:///home/rony/LPR/AdMetricsPro/docs/adr/0020-zero-direct-db-access-frontend-api-client.md) | Frontend Estritamente de Apresentação via Clientes Web API Fortemente Tipados | Aceito | 2026-09-07 |
| [0021](file:///home/rony/LPR/AdMetricsPro/docs/adr/0021-squads-and-portfolio-isolation.md) | Gestão de Squads e Isolamento Granular de Portfólios por Cliente | Aceito | 2026-09-07 |
| [0022](file:///home/rony/LPR/AdMetricsPro/docs/adr/0022-agency-ftux-and-interactive-onboarding-wizard.md) | Wizard de Primeiro Acesso da Agência (FTUX) e Dados Demonstrativos | Aceito | 2026-09-07 |
| [0023](file:///home/rony/LPR/AdMetricsPro/docs/adr/0023-transactional-emails-and-trial-lifecycle-messaging.md) | Mensageria Transacional e Régua Automatizada de Ciclo de Vida do Trial | Aceito | 2026-09-07 |
| [0024](file:///home/rony/LPR/AdMetricsPro/docs/adr/0024-payment-gateway-and-trial-to-paid-transition.md) | Gateway de Pagamentos e Transição de Trial para Assinatura Paga | Aceito | 2026-09-07 |
| [0025](file:///home/rony/LPR/AdMetricsPro/docs/adr/0025-dynamic-cname-resolution-and-white-label-strategy.md) | Estratégia de Resolução Dinâmica de CNAME e Customização White-Label | Aceito | 2026-09-15 |
| [0026](file:///home/rony/LPR/AdMetricsPro/docs/adr/0026-oauth2-hub-and-token-vault-encryption.md) | OAuth2 Hub e Token Vault Criptografado | Aceito | 2026-09-15 |
| [0027](file:///home/rony/LPR/AdMetricsPro/docs/adr/0027-campaign-structural-sync-and-rate-limiting.md) | Sincronização Estrutural de Campanhas e Rate Limiting | Aceito | 2026-09-15 |
| [0028](file:///home/rony/LPR/AdMetricsPro/docs/adr/0028-campaign-metrics-ingestion-and-idempotency.md) | Ingestão de Métricas de Campanha e Idempotência | Aceito | 2026-09-15 |
| [0029](file:///home/rony/LPR/AdMetricsPro/docs/adr/0029-currency-normalization-and-automated-taxonomy.md) | Normalização Cambial e Taxonomia Automatizada | Aceito | 2026-09-15 |
| [0030](file:///home/rony/LPR/AdMetricsPro/docs/adr/0030-blended-metrics-and-multichannel-attribution.md) | Blended Metrics e Atribuição Multicanal | Aceito | 2026-09-15 |
| [0031](file:///home/rony/LPR/AdMetricsPro/docs/adr/0031-unified-blazor-executive-dashboard.md) | Dashboard Executivo Unificado em Blazor | Aceito | 2026-09-16 |
| [0032](file:///home/rony/LPR/AdMetricsPro/docs/adr/0032-cross-platform-rules-dsl-and-action-dispatcher.md) | DSL de Regras Cross-Platform e Despachador de Ações | Aceito | 2026-09-16 |
| [0033](file:///home/rony/LPR/AdMetricsPro/docs/adr/0033-safety-guards-overspending-and-broken-links.md) | Travas de Segurança: Overspending e Links Quebrados | Aceito | 2026-09-16 |
| [0034](file:///home/rony/LPR/AdMetricsPro/docs/adr/0034-budget-pacing-and-month-end-forecasting.md) | Budget Pacing e Projeção de Consumo de Verba | Aceito | 2026-09-16 |
| [0035](file:///home/rony/LPR/AdMetricsPro/docs/adr/0034-bulk-cross-platform-campaign-operations.md) | Operações e Edição em Massa Multiplataforma | Aceito | 2026-09-16 |
| [0036](file:///home/rony/LPR/AdMetricsPro/docs/adr/0036-ai-copilot-traffic-auditor-and-anomaly-detection.md) | Copiloto de Otimização via IA (Auditor de Tráfego) e Detecção de Anomalias | Aceito | 2026-09-16 |
| [0037](file:///home/rony/LPR/AdMetricsPro/docs/adr/0037-white-label-automated-reports-generator.md) | Gerador de Relatórios Automatizados em White-Label e Disparador Multi-Canal | Aceito | 2026-09-16 |
| [0038](file:///home/rony/LPR/AdMetricsPro/docs/adr/0038-production-docker-orchestration.md) | Orquestração de Produção com Docker Compose, Multi-Stage .NET 10 e Reverse Proxy Nginx | Aceito | 2026-09-16 |

---

## Governança e Regras de Atualização

1. Toda nova decisão de impacto estrutural (ex: introdução de mensageria externa distribuída, replicação geográfica de dados ou novos provedores de nuvem) deve ser formalizada como um novo ADR sequencial.
2. Cada ADR deve seguir rigorosamente as quatro seções: **Status**, **Contexto**, **Decisão** e **Consequências** (Positivas e Negativas/Mitigações).
3. Alterações em decisões anteriores devem ser registradas em um novo ADR que declara o status "Substitui ADR XXXX".
