# Mensageria durável (eventos de integração)

> **Fases 0–4:** outbox, dispatcher, inbox, transporte MassTransit, consumidor de laboratório (testes) e **consumidor de e-mail de catálogo** (boas-vindas e confirmação de assinatura) quando `Messaging__Enabled=true`. Sem endpoint HTTP de mensageria. Decisão: [ADR 0052](../adr/0052-rabbitmq-integration-events-and-transactional-outbox.md); ordem: [roadmap](../roadmaps/rabbitmq-messaging-roadmap.md).

## 1. Fase 1 — broker no ambiente (entregue)

| Item | Comportamento |
| :--- | :--- |
| Compose (dev) | Serviço `rabbitmq:4-management`; portas `5672` e `15672` apenas em `127.0.0.1`. |
| Compose (prod) | `rabbitmq:4` sem management; **nenhuma** porta AMQP publicada no host. |
| `Messaging__Enabled=false` (padrão) | Health check `rabbitmq` **não** registrado; `GET /health` segue 200; nenhum handler publica mensagem. |
| `Messaging__Enabled=true` | Health check nomeado `rabbitmq` no `GET /health`; broker inacessível → agregado Unhealthy (503); registra `AddAdMetricsMessaging` e opcionalmente `OutboxDispatchWorker`. |
| `Messaging__Dispatch__Enabled=false` (padrão implícito) | Com `Messaging__Enabled=true`, o worker existe mas não publica sem dispatch ligado (`Messaging.Disabled` no ciclo). |
| `Messaging__Dispatch__IntervalSeconds` | Intervalo do `BackgroundService` (mínimo **5**). |
| `GET /api/v1/health` | Inalterado (contrato de negócio sempre `Healthy`). |
| Variáveis | `RabbitMQ__*`, `Messaging__HostRole` (lido; papel efetivo na Fase 10), `Messaging__Dispatch__*`, ver [.env.example](../../.env.example). |

## 2. Diagnóstico do estado atual

A comunicação entre módulos é **in-memory** via MediatR ([ADR 0009](../adr/0009-in-memory-messaging-and-validation-pipeline.md)): `ICommand` / `IQuery`, `ValidationBehavior` e `DomainEventNotification<T>` para `IDomainEventHandler<T>`. Isso permanece correto quando o caller precisa de `Result<T>` na mesma requisição HTTP.

Três lacunas motivam o canal durável:

| Lacuna | Sintoma | ADR / artefato relacionado |
| :--- | :--- | :--- |
| Publish pós-commit | Crash ou falha do handler após `CommitAsync` perde o evento (ex.: `CampaignMetricsSyncedEvent`, eventos do `SocialPublishingUnitOfWork`) | ADR 0009 (limite in-process) |
| Trabalho longo na API | `MetaSynchronizationWorker`, dunning, trial e dispatch orgânico rodam no mesmo processo da Web API; réplicas duplicam laços | — |
| Outbox local sem transporte | `PublicationJob` tem outbox SQL com claim e retry, mas não atravessa processo | [ADR 0046](../adr/0046-organic-publication-outbox-and-dispatch-worker.md) |

O frontend (`WebApp`, `BackofficeApp`) continua **somente** via Web API ([ADR 0020](../adr/0020-zero-direct-db-access-frontend-api-client.md)); nenhum cliente AMQP na UI.

## 3. Fronteira: MediatR vs fila

| Situação | Canal | Motivo |
| :--- | :--- | :--- |
| CRUD, login, RBAC, consulta de dashboard, validação de formulário | MediatR, resposta HTTP | O caller precisa do `Result<T>` na hora. |
| Evento de domínio cujo handler só altera estado já aberto na mesma transação | MediatR, antes ou durante o `CommitAsync` | Consistência local. Não atravessa processo. |
| E-mail transacional (boas-vindas, confirmação, trial, dunning, convite) | Outbox do `MasterDb` → RabbitMQ | SMTP lento e falível; idempotência via `TenantNotificationLog`. |
| Sync de hierarquia e métricas por conta | Scheduler enfileira uma mensagem por conta | Comandos `SyncCampaignHierarchy` / `SyncCampaignMetrics` permanecem. |
| `CampaignMetricsSynced` / `CampaignHierarchySynced` | Outbox do tenant, mesmo commit da ingestão | Pacing, travas e regras precisam de garantia pós-commit. |
| Publicação orgânica no horário | `PublicationJob` é a agenda; fila só executa job devido | ADR 0046; a fila não vira agendador. |
| Geração de cópia/imagem, PDF, relatório | HTTP grava job + outbox e retorna id | Evita segurar request Blazor em APIs externas. |
| Operação em massa com chamada de rede | Uma mensagem por item, com inbox | Falha parcial tolerada (bulk cross-platform). |
| Migração de schema no startup | Hosted service atual | Não é mensagem de negócio. |

## 4. Fase 2 — Contratos (entregue, sem broker)

Tipos em código (sem pacote MassTransit / `RabbitMQ.Client`):

| Tipo | Projeto | Papel |
| :--- | :--- | :--- |
| `IIntegrationEvent` | `BuildingBlocks.Application` | Marcador do fato já commitado (distinto de `IDomainEvent`). |
| `IntegrationEventEnvelope` | `BuildingBlocks.Application` | Envelope validado via `Result<IntegrationEventEnvelope>`; falhas usam `Messaging.PayloadInvalid` / `Messaging.TenantRequired`. |
| `IntegrationEventScope` | `BuildingBlocks.Application` | `Catalog` (Master; `tenantId` opcional) ou `Operational` (`tenantId` obrigatório na validação). |
| `IntegrationEventEnvelopeSerializer` | `BuildingBlocks.Application` | JSON com nomes fixos em camelCase (`eventId`, `messageType`, …); `payload` é objeto JSON, não string escapada. |
| `IntegrationOutboxMessage` | `BuildingBlocks.Domain` | Entidade outbox (`Pending`, `InProgress`, `Published`, `DeadLetter`); transições `MarkInProgress`, `MarkPublished`, `Requeue`, `MarkDeadLetter`, `Reclaim`. |
| `IntegrationInboxMessage` | `BuildingBlocks.Domain` | Idempotência de transporte (`EventId` + `ConsumerName`). |
| `MessagingSchedulerLease` | `BuildingBlocks.Domain` | Lease única `outbox-dispatch` no `MasterDb`. |
| `BuildingBlocks.Messaging` | Transporte | MassTransit + `IIntegrationEventPublisher`; filtro de payload/tenant; **não** referenciado por Domain/Application/UI. |
| `ITransientIntegrationFailure` | `BuildingBlocks.Domain` | Marca falha transitória (429, SMTP temporário) para reagendamento via outbox. |
| `IntegrationResultDisposition` | `BuildingBlocks.Application` | Política pura de ack a partir de `Result` (ver seção 6). |

Representação do payload publicado no exchange `admetricspro.integration` (Fase 3+). Cabeçalhos AMQP espelham os mesmos campos (`message-id` = `eventId`, `correlation-id`, `message-type`, `occurred-on-utc`, `tenant-id` quando aplicável).

**Regras de validação (`IntegrationEventEnvelope.Create`):**

- `eventId` e `correlationId` (GUID) obrigatórios e não vazios.
- `messageType` não vazio, versionado (ex.: `integrations.campaign-metrics-synced.v1`).
- `payload` deve ser objeto JSON (`JsonValueKind.Object`).
- Escopo **operacional:** `tenantId` obrigatório → falha `Messaging.TenantRequired` (consumidor envia para dead-letter na Fase 3).
- Escopo **catálogo:** `tenantId` nulo permitido; informar `tenantId` (ex.: e-mail de boas-vindas) também é aceito.
- **Proibido** no JSON: tokens OAuth, connection strings, senhas, corpo HTML de e-mail.

**Outbox (entidade):** `IntegrationOutboxMessage.Create` exige `eventId` e `messageType` válidos; status inicial `Pending`, `attemptCount` 0, `nextAttemptAtUtc` nulo. Reagendamento transitório **atualiza a mesma linha** (`Pending` + `NextAttemptAtUtc` + `AttemptCount`); não há segunda linha com o mesmo `EventId`.

**Exemplo de envelope (ilustrativo):**

```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "messageType": "integrations.campaign-metrics-synced.v1",
  "correlationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "occurredOnUtc": "2026-11-25T18:30:00.000Z",
  "tenantId": "2f8b3c1a-9d4e-4b7c-a1e2-3f4d5e6a7b8c",
  "payload": {
    "connectedAdAccountId": "act_123456789",
    "platform": "Meta",
    "metricsWindowStartUtc": "2026-11-24T00:00:00.000Z",
    "metricsWindowEndUtc": "2026-11-25T00:00:00.000Z"
  }
}
```

## 4.1 Fase 3 — Persistência SQL (entregue)

Schema **`messaging`** no `MasterDb` e no banco operacional do tenant (`TenantDbContext` / `TenantOperationalDbContext`):

| Objeto | Master | Tenant | Observação |
| :--- | :---: | :---: | :--- |
| `messaging.IntegrationOutboxMessages` | ✓ | ✓ | Índice único em `EventId`; índice de despacho `(Status, NextAttemptAtUtc)`; claim com `RowVersion`. |
| `messaging.IntegrationInboxMessages` | ✓ | ✓ | Índice único `(EventId, ConsumerName)`. |
| `messaging.MessagingSchedulerLeases` | ✓ | — | Linha `outbox-dispatch`; lease com expiração e renovação. |

Portas (`BuildingBlocks.Application`): `IIntegrationOutboxStore`, `IIntegrationInboxStore`, `IMessagingSchedulerLeaseStore`, `IIntegrationEventPublisher`. Implementação EF em `BuildingBlocks.Infrastructure`. O dispatcher (`OutboxDispatchWorker`, `WebApi/Background`) só roda com `Messaging:Enabled=true`; cada ciclo respeita `Messaging:Dispatch:Enabled`, kill switch `killswitch.messaging.dispatch`, lease no Master, claim no catálogo e nos tenants `Active`/`Trial`, publicação e só então `MarkPublished`. Falha de publicação → `Requeue` na mesma linha. `InProgress` expirado → `Reclaim` → `Pending`.

**Consumidor e inbox:** ack terminal (sucesso ou erro permanente coberto por política) grava inbox na mesma transação do efeito; reentrega com mesmo `EventId` não repete o handler (`IntegrationResultDisposition` + `ExistsAsync`). Reagendar publicação **não** grava inbox.

**Laboratório:** tipo `messaging.ping.v1` e consumidor `PingIntegrationMessageConsumer` existem apenas em `tests/IntegrationTests`.

### 4.2 Fase 4 — E-mail transacional (catálogo)

| Item | Valor |
| :--- | :--- |
| Routing keys | `master.tenant-welcome-email.v1`, `master.subscription-confirmation-email.v1` |
| Fila | `admetricspro.master.catalog-email` |
| Consumidor | `CatalogEmailIntegrationConsumer` (`WebApi/Messaging`) |
| Inbox / outbox | `MasterIntegrationInboxStore` + reagendamento na mesma linha via `ScheduleForConsumerRetry` (de `Published` → `Pending`) |
| Comandos | `SendTenantWelcomeEmailCommand`, `SendSubscriptionConfirmationEmailCommand` |
| Gravação outbox | `ICatalogIntegrationOutboxWriter` (provisionamento e checkout/webhook pagos no mesmo `CommitAsync` do Master) |
| `Messaging__Enabled=false` | Boas-vindas via `TenantProvisionedEvent` + `SendTenantWelcomeEmailCommand` in-process; confirmação de assinatura **não** enfileirada (comportamento legado de checkout) |
| Retry SMTP longo | `Email.TransientSendFailed` (`ITransientIntegrationFailure`); teto `Messaging:Consumers:CatalogEmail:MaxAttempts` (padrão 5) |

## 5. Fluxo alvo (referência)

```mermaid
sequenceDiagram
    participant HTTP as Controller_or_Scheduler
    participant MED as MediatR
    participant UoW as UnitOfWork
    participant SQL as SQL_Server
    participant DSP as OutboxDispatcher
    participant MQ as RabbitMQ
    participant CON as Consumer

    HTTP->>MED: Comando síncrono
    MED->>UoW: Negócio + linha outbox
    UoW->>SQL: CommitAsync
    UoW-->>HTTP: Result sucesso
    DSP->>SQL: Claim RowVersion
    DSP->>MQ: Publish envelope
    DSP->>SQL: Marca Published
    MQ->>CON: Entrega
    CON->>CON: Restaura TenantContext
    CON->>MED: Comando existente + inbox
```

## 6. Política de ack a partir de `Result`

Implementação: `IntegrationResultDisposition.ForResult` / `ForUnexpectedFailure` (`BuildingBlocks.Application.Messaging`). Ordem de avaliação:

1. `EventId` já na inbox → **Acknowledge** (idempotência de reentrega).
2. `Result` sucesso → **Acknowledge**.
3. `ErrorType` `Validation`, `NotFound`, `Conflict`, `Unauthorized` ou `Forbidden` → **Acknowledge** (sem retry de transporte).
4. `Error` que implementa `ITransientIntegrationFailure` → **Reschedule** com `NextAttemptAtUtc` (mesma linha de outbox, backoff).
5. `ErrorType.Failure` sem marcador transitório → **Acknowledge** (falha de negócio explícita, ex. regra violada).
6. Exceção inesperada no consumidor → **RetryTransport** (retry curto MassTransit na Fase 3; depois dead-letter).

| Resultado | Ação (`IntegrationMessageDispositionAction`) |
| :--- | :--- |
| Sucesso | `Acknowledge`; grava inbox (Fase 3). |
| `Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden` | `Acknowledge`; log estruturado; sem retry. |
| `ITransientIntegrationFailure` (429, SMTP temporário, etc.) | `Reschedule` com `NextAttemptAtUtc`. |
| `Failure` sem marcador transitório | `Acknowledge`. |
| Exceção inesperada | `RetryTransport`; depois dead-letter. |
| `EventId` já na inbox | `Acknowledge` imediato. |

## 7. Catálogo de erros do módulo

| Código | Quando | Destino / efeito |
| :--- | :--- | :--- |
| `Messaging.Disabled` | `Messaging__Enabled=false` | Dispatcher não publica; linha outbox permanece `Pending`. |
| `Messaging.TenantRequired` | Evento operacional sem `TenantId` no envelope | Dead-letter; consumidor não abre `DbContext` de tenant. |
| `Messaging.PayloadInvalid` | `EventId` ausente, tipo desconhecido ou JSON incompatível com a versão | Dead-letter. |
| `Messaging.LeaseNotAcquired` | Outra instância detém lease do scheduler | Esta instância não varre outbox; sem publicação neste ciclo. |
| `Messaging.PublishFailed` | Broker recusa publicação | Linha volta a `Pending` com backoff. |

Consumidor **não** usa exceção para forçar retry em falha de negócio coberta por `Result`.

## 8. Fora de escopo (fronteira protegida)

- Substituir MediatR por fila em comando ou consulta síncrona.
- Colocar segredos ou credenciais no corpo da mensagem.
- Revogar `PublicationJob` como agenda ou reescrever `TenantNotificationLog` / chave de `CampaignMetric`.
- Cliente RabbitMQ no Blazor, Backoffice ou browser.
- Endpoints HTTP de mensageria **nesta fase** (consulta de dead-letter e status de job entram em fases posteriores).

## 9. Referências

- [ADR 0052 — RabbitMQ e outbox transacional](../adr/0052-rabbitmq-integration-events-and-transactional-outbox.md)
- [Roadmap de implementação](../roadmaps/rabbitmq-messaging-roadmap.md)
- [ADR 0009 — MediatR in-memory](../adr/0009-in-memory-messaging-and-validation-pipeline.md)
- [ADR 0046 — PublicationJob](../adr/0046-organic-publication-outbox-and-dispatch-worker.md)