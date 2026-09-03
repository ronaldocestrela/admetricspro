# ADR 0012: Mecanismo Seguro de Impersonation (Shadow Mode) e Proteção de Dados Sensíveis

## Status
Aceito

## Contexto
Técnicos de suporte e operadores de nível 1/super admins necessitam acessar o ambiente dos clientes (tenants) para diagnosticar divergências de relatórios, verificar configurações de regras e reproduzir incidentes de plataforma. 

Conforme especificado em `docs/functions/Backoffice-Functions.md` (Seção 2.2) e no `Implementation-Roadmap-backoffice.md` (Fase 4), o acesso técnico não pode depender do fornecimento ou redefinição de senhas do cliente final. Além disso:
1. Deve exigir justificativa explícita e número de ticket de suporte antes de qualquer emissão de sessão.
2. Deve emitir tokens JWT contextuais com claims de auditoria (`is_impersonated=true`, `original_superadmin_id`, `tenant_id`, `support_ticket`, `impersonation_session_id`).
3. Deve impor uma política rigorosa de segurança de dados bancários e faturamento (Shadow Mode), mascarando números de cartão de crédito (`**** **** **** 1234`), documentos fiscais (CPF/CNPJ) e informações bancárias confidenciais enquanto o modo personificação estiver ativo.

## Decisão

1. **Modelagem de Domínio no Módulo Master (`Master.Domain`):**
   - Criação do aggregate root `ImpersonationSession` herdando de `AggregateRoot<Guid>`.
   - Encapsulamento das propriedades de auditoria: `TenantId`, `SuperAdminId`, `SupportTicketId`, `Reason`, `CreatedAtUtc`, `ExpiresAtUtc`, `RevokedAtUtc`, `RevokeReason`.
   - Método estático `ImpersonationSession.Create(...)` garantindo validação de justificativa (mínimo 10 caracteres), presença de ticket de suporte e duração temporal entre 5 e 120 minutos.
   - Suporte à revogação explícita de sessão via método `Revoke(...)`.
   - Catálogo de erros de domínio em `ImpersonationErrors`.

2. **Abstrações e Contratos no Kernel (`BuildingBlocks.Application` & `BuildingBlocks.Infrastructure`):**
   - Definição das constantes de claims em `ImpersonationClaims`.
   - Criação das interfaces `IImpersonationContext` e `IImpersonationContextAccessor`, permitindo que qualquer módulo ou serviço avalie se a requisição atual é executada em modo personificação.
   - Implementação do serviço `IBillingDataMasker` e `BillingDataMasker`, oferecendo mascaramento inteligente para cartões de crédito, CPF, CNPJ e contas bancárias, condicionado à flag `IsImpersonated`.
   - Extensão `AddSecurityServices` para injeção de dependências no pipeline HTTP.

3. **Orquestração e Emissão de Tokens (`Master.Application` & `Master.Infrastructure`):**
   - Criação do comando `ImpersonateTenantCommand`, validador FluentValidation `ImpersonateTenantCommandValidator` e handler `ImpersonateTenantCommandHandler`.
   - Serviço `JwtImpersonationTokenService` gerando e validando tokens JWT assinados com algoritmo HMAC-SHA256, contendo todas as claims de impersonação e expiração estrita.
   - Repositório `IImpersonationSessionRepository` e implementação EF Core `ImpersonationSessionRepository`.

4. **Persistência e Migrações no Catálogo Master (`MasterDb`):**
   - Mapeamento EF Core `ImpersonationSessionEntityTypeConfiguration` mapeando a tabela `ImpersonationSessions` com índices em `TenantId`, `SuperAdminId` e `SupportTicketId`.
   - Migração versionada `20260903150000_Add_ImpersonationSessions`.

5. **Exposição de API e Contratos (`WebApi`):**
   - Controlador `TenantsController` expondo o endpoint `POST /api/v1/tenants/{tenantId}/impersonate`.
   - Envelope `Result<ImpersonateTenantResponse>` com mapeamento semântico para códigos HTTP (200, 400, 404, 422).
   - Metadados OpenAPI e Scalar (`[EndpointSummary]`, `[ProducesResponseType]`).

6. **Serialização Segura do Padrão `Result<T>`:**
   - Criação do `ResultJsonConverterFactory` em `BuildingBlocks.Domain.Primitives` para serializar e deserializar `Result` e `Result<TValue>` em JSON, garantindo que em resultados com falha o campo `value` seja emitido como `null` sem disparar a exceção de acesso à propriedade `Result<TValue>.Value`.

## Consequências

### Positivas
- **Auditoria Transparente e Rastreabilidade Total:** Nenhuma personificação ocorre sem registro de ticket e identificador do SuperAdmin responsável.
- **Proteção de Dados do Cliente (LGPD / PCI-DSS):** Dados de pagamento e cartões são automaticamente ofuscados quando a requisição é identificada como `is_impersonated=true`.
- **Validade Temporal Curta e Revogabilidade:** Tokens possuem validade restrita (padrão 30 min, máx 120 min) e sessões podem ser revogadas no banco central a qualquer momento.
- **Isolamento Modular:** Módulos operacionais consomem `IImpersonationContext` via Kernel compartilhado sem acoplamento direto com o `MasterDb`.
- **Cobertura TDD Integral:** 100% de cobertura com testes unitários e testes de aceitação de endpoint.

### Negativas / Mitigações
- Tokens JWT gerados em modo stateless não verificam o banco a cada requisição HTTP interna por padrão.
  - *Mitigação:* Duração curta do token (máximo 120 minutos) e o accessor pode validar o status da sessão via cache ou base de dados em endpoints de risco crítico.
