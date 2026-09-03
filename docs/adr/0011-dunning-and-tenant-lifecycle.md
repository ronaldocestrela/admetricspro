# ADR 0011: Régua de Inadimplência e Bloqueio Funcional Progressivo (Dunning Engine)

## Status
Aceito

## Contexto
O AdMetricsPro é uma plataforma SaaS multitenant de gestão unificada de tráfego pago com modelo de faturamento recorrente. A governança financeira e o controle de churn exigem uma política clara e automatizada para o tratamento de inadimplência (falhas de pagamento de assinaturas e faturas vencidas).

No documento de requisitos funcionais (`docs/functions/Backoffice-Functions.md`, Seção 3.2) e no roadmap de implementação (`docs/roadmaps/Implementation-Roadmap-backoffice.md`, Subfase 3.2), é estabelecido que a plataforma não deve suspender imediatamente o acesso do cliente ao primeiro dia de atraso, mas sim aplicar uma **política de bloqueio progressivo** que resguarde as operações críticas de tráfego sem gerar inadimplência descontrolada:
1. **D+0 a D+2 (Período de Tolerância / Grace Period):** Alertas operacionais e cobranças automáticas sem bloqueios na interface ou na ingestão.
2. **D+3 a D+6 (Estágio 1 - Desativação de Automações):** Desativação do motor de regras automáticas cross-network e pausas de regras para evitar estouro de verba sem governança.
3. **D+7 a D+13 (Estágio 2 - Bloqueio de Relatórios):** Restrição a consultas de métricas, relatórios avançados, dashboards analíticos e exportações de relatórios.
4. **D+14+ (Estágio 3 - Bloqueio Total de Login e Suspensão):** Bloqueio integral de autenticação de usuários da organização e marcação formal do status da empresa como `Suspended`.
5. **Regularização:** Ao confirmar o pagamento pendente, o tenant deve ser reativado imediatamente (`DunningStage.None`, `TenantStatus.Active`).

## Decisão

1. **Modelagem no Módulo Master (`Master.Domain`):**
   - Criação do enum `DunningStage` (`None`, `AutomationsDisabled`, `ReportsBlocked`, `LoginBlocked`).
   - Introdução da política pura de domínio `DunningPolicy` contendo os limiares temporais (`3`, `7` e `14` dias) e métodos de checagem de permissão funcional (`AreAutomationsAllowed`, `AreReportsAllowed`, `IsLoginAllowed`).
   - Extensão do agregado `Tenant` para encapsular `DunningStage` e `PaymentDueDateUtc`, fornecendo os métodos de mutação de estado `MarkPaymentOverdue(DateTime dueDateUtc)`, `EvaluateDunningStage(DateTime referenceUtc)` e `RegularizePayment()`.
   - Emissão do evento de domínio imutável `TenantGracePeriodExceededEvent` sempre que o período de tolerância for superado ou o estágio for alterado.

2. **Orquestração na Camada de Aplicação (`Master.Application`):**
   - Criação da interface `IDunningEngineService` e implementação `DunningEngineService`.
   - O serviço avalia os tenants inadimplentes, consolida as alterações de estado via `IUnitOfWork.CommitAsync` e despacha notificações de eventos de domínio em memória via `IPublisher` (`MediatR`).
   - Disponibilização do comando `ExecuteDunningCycleCommand` para acionamento manual ou programado.
   - Handler desacoplado `TenantGracePeriodExceededEventHandler` para logs estruturados e extensibilidade para integração com módulos de notificação e automações.

3. **Background Service In-Memory (`Master.Infrastructure`):**
   - Implementação de `DunningBackgroundService` herdando de `BackgroundService`.
   - Execução em ciclo contínuo em segundo plano (com intervalo parametrizado via `DunningOptions`, padrão de 24h).
   - Uso de `IServiceScopeFactory` para instanciar de forma isolada os serviços da aplicação e evitar memory leaks.

4. **Persistência no Catálogo Master (`MasterDb`):**
   - Atualização do mapeamento EF Core em `TenantEntityTypeConfiguration`.
   - Criação da migração versionada `20260903140000_Add_TenantDunningStage` adicionando as colunas `DunningStage` (nvarchar(30)) e `PaymentDueDateUtc` (datetime2 nullable) na tabela `Tenants`.

5. **Exposição de API e Scalar UI:**
   - Criação do endpoint `POST /api/v1/billing/dunning/execute` no `BillingController` retornando envelope `Result<DunningExecutionSummaryResponse>` devidamente documentado com metadados OpenAPI.

## Consequências

### Positivas
- **Prevenção Gradual de Riscos:** Mitiga o cancelamento abrupto (churn involuntário) permitindo regularização em D+0..D+2 e D+3..D+6 antes de medidas drásticas.
- **Segurança Operacional:** Desativa automações em D+3 para prevenir que regras continuem executando ações orçamentárias na conta de anúncios de um cliente inadimplente.
- **Desacoplamento e Conformidade Arquitetural:** Respeita o padrão de monólito modular do `AGENTS.md`, disparando eventos de domínio em memória e tratando fluxo sob o envelope `Result<T>` sem lançar exceções.
- **Testabilidade Integral:** Cobertura TDD completa em testes unitários, testes de integração com banco SQL Server real (Testcontainers) e testes de aceitação de API HTTP.

### Negativas / Mitigações
- Avaliação de grandes volumes de tenants em lote no background pode gerar lock na tabela `Tenants`.
  - **Mitigação:** A consulta filtra estritamente tenants com `PaymentDueDateUtc != null` ou `DunningStage != DunningStage.None`, minimizando o conjunto de dados carregado.
