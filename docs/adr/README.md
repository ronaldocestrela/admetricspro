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

---

## Governança e Regras de Atualização

1. Toda nova decisão de impacto estrutural (ex: introdução de mensageria externa distribuída, replicação geográfica de dados ou novos provedores de nuvem) deve ser formalizada como um novo ADR sequencial.
2. Cada ADR deve seguir rigorosamente as quatro seções: **Status**, **Contexto**, **Decisão** e **Consequências** (Positivas e Negativas/Mitigações).
3. Alterações em decisões anteriores devem ser registradas em um novo ADR que declara o status "Substitui ADR XXXX".
