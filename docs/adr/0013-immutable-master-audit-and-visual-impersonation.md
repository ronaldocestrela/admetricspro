# ADR 0013: Trilha de Auditoria Master Imutável e Sinalização Visual de Impersonation

## Status
Aceito

## Contexto
Na Subfase 4.1, foram estabelecidos a emissão de tokens JWT contextuais (`is_impersonated=true`) e o mascaramento de dados confidenciais (cartão de crédito, CPF, CNPJ, dados bancários). 

Para cumprir os requisitos de conformidade (SOC 2, LGPD e PCI-DSS) e as diretrizes de governança em `docs/functions/Backoffice-Functions.md` (Seção 2.2 e 6) e `Implementation-Roadmap-backoffice.md` (Subfase 4.2):
1. Todas as mutações e comandos executados em nome do cliente devem ser imutavelmente auditados no Catálogo Central (`MasterDb`) com a tag permanente `performed_by_superadmin`.
2. As operações de auditoria não devem bloquear os módulos transacionais e devem registrar obrigatoriamente `SuperAdminId`, `SupportTicketId` e `ImpersonationSessionId`.
3. O operador não deve perder a visibilidade de que está atuando em nome de um cliente real; uma tarja de alto contraste com botão de encerramento imediato deve permanecer visível durante toda a sessão.

## Decisão

1. **Entidade de Domínio Imutável no Módulo Master (`Master.Domain.Auditing`):**
   - Criação da entidade agregada `MasterAuditEntry`, concebida como estritamente *append-only* (sem rotinas de alteração ou exclusão).
   - Validações de domínio exigindo a presença de identificadores do operador (`SuperAdminId`, `SupportTicketId`, `ImpersonationSessionId`) quando `isImpersonated == true`.
   - Adição automática da tag `performed_by_superadmin` (`MasterAuditTags.PerformedBySuperadmin`).

2. **Persistência EF Core e Índices Otimizados (`MasterDb`):**
   - Mapeamento na tabela `MasterAuditLogs` via `MasterAuditEntryEntityTypeConfiguration`.
   - Índices compostos por `(TenantId, CreatedAtUtc)`, `(SuperAdminId, CreatedAtUtc)`, `(IsImpersonated, CreatedAtUtc)` e índice simples em `Action`.
   - Migração versionada `20260903160000_Add_MasterAuditLogs` aplicada no startup e provisionamento.

3. **Interceptação e Rastreamento Desacoplado:**
   - Implementação de `IMasterAuditRepository` e `MasterAuditRepository`.
   - Implementação de `IMasterAuditService` e `MasterAuditService` no `Master.Application`.
   - Pipeline Behavior do MediatR `AuditImpersonationBehavior<TRequest, TResponse>`, interceptando comandos executados com `IImpersonationContext.IsImpersonated == true`.

4. **Comando de Encerramento Imediato de Sessão:**
   - Comando `TerminateImpersonationSessionCommand`, validador `TerminateImpersonationSessionCommandValidator` e handler `TerminateImpersonationSessionCommandHandler`.
   - Endpoint HTTP `POST /api/v1/tenants/{tenantId}/impersonate/{sessionId}/terminate` documentado via OpenAPI/Scalar e retornando padrão `Result`.

5. **Interface e Sinalização Visual Blazor (`WebApp`):**
   - Componente `ImpersonationBanner.razor` posicionado no topo de `MainLayout.razor`.
   - Estilização em `ImpersonationBanner.razor.css` com paleta de advertência, indicador de pulso vermelho e badge "MODO SHADOW ATIVO".
   - Estado gerenciado via `ImpersonationSessionState` e `IImpersonationStateProvider`.
   - Consumo do encerramento de sessão via `IImpersonationClientService` injetado via DI.

## Consequências

### Positivas
- **Imutabilidade e Compliance Total:** Registro inviolável de qualquer intervenção em contas de clientes, garantindo trilha forense completa com identificação do ticket de suporte.
- **Transparência e Prevenção de Incidentes:** A tarja permanente no topo da interface elimina o risco de operadores executarem modificações inadvertidas acreditando estar no próprio ambiente ou sem contexto de suporte.
- **Encerramento Imediato:** Capacidade de revogar a sessão a qualquer momento com feedback em tempo real e atualização de estado no backend.
- **Cobertura TDD 100%:** Testes unitários de domínio, testes de repositório, testes de pipeline de migração, testes de aceitação de endpoint e testes bUnit de componentes frontend.

### Negativas / Mitigações
- Operações intensivas sob impersonation geram registros extras em `MasterAuditLogs`.
  - *Mitigação:* Tabela com índices compostos específicos para consultas de suporte e temporalidade, além de campos de tamanho limitado para otimização de I/O.
