# 0052 — Eventos de integração duráveis com RabbitMQ e outbox transacional

## Status

Aceito em 2026-11-25.

As **Fases 1–3** do [roadmap de mensageria](../roadmaps/rabbitmq-messaging-roadmap.md) estão implementadas: broker opcional, contratos, schema SQL, dispatcher com lease, transporte MassTransit em `BuildingBlocks.Messaging` e circuito de laboratório (ping só em testes). Consumidores de negócio e papéis de host efetivos seguem nas fases 4+. Este ADR não revoga o [ADR 0009](0009-in-memory-messaging-and-validation-pipeline.md) nem o [ADR 0046](0046-organic-publication-outbox-and-dispatch-worker.md).

**Nomes físicos (Fase 3):** schema `messaging`; tabelas `IntegrationOutboxMessages` e `IntegrationInboxMessages` no `MasterDb` e no banco do tenant; `MessagingSchedulerLeases` apenas no `MasterDb`; inbox única em `(EventId, ConsumerName)`. Exchange topic `admetricspro.integration`; dead-letter `admetricspro.dead-letter`. Detalhes em [módulo messaging](../modules/messaging.md).

## Contexto

O AdMetricsPro comunica módulos via MediatR in-process ([ADR 0009](0009-in-memory-messaging-and-validation-pipeline.md)). Comandos, consultas, validação com `Result<T>` e eventos de domínio na mesma requisição permanecem adequados. Três lacunas aparecem quando o efeito precisa sobreviver ao processo, ser reexecutado ou distribuir-se entre instâncias:

1. **Publicação depois do commit, sem durabilidade.** Handlers como `SyncCampaignMetricsCommandHandler` e `SocialPublishingUnitOfWork` fazem `CommitAsync` / `SaveChangesAsync` e só então `IPublisher.Publish`. Um crash entre o commit e o publish perde o evento; falha do handler in-memory não retenta após o dado já persistido.
2. **Trabalho longo no host da API.** `MetaSynchronizationWorker`, `SocialPublishingDispatchWorker`, `DunningBackgroundService` e `TrialNoticeBackgroundService` rodam no mesmo processo da Web API. Escalar a API duplica laços sem lease global; só `PublicationJob` ([ADR 0046](0046-organic-publication-outbox-and-dispatch-worker.md)) tem claim otimista por `RowVersion`.
3. **Outbox local que não atravessa processo.** `social.PublicationJobs` é outbox SQL com idempotência e `NextAttemptAtUtc`, mas não cruza fronteira de processo nem instância. O ADR 0046 deixou fila externa fora de propósito; o roadmap **consome** essa tabela na Fase 7, sem substituí-la como agenda.

O isolamento [database-per-tenant](0002-database-per-tenant-strategy.md) com catálogo no `MasterDb` impede adotar o **outbox transacional único do MassTransit** preso a um só `DbContext`: eventos de catálogo e operacionais precisam de dois repositórios de outbox (Master e tenant) e um único broker RabbitMQ na borda.

## Decisão

Introduzir **mensageria durável** com **outbox transacional** em SQL e transporte **RabbitMQ** via **MassTransit** restrito à infraestrutura, mantendo MediatR e as outboxes já aceitas.

### Fronteira MediatR × fila

- **MediatR** continua o mediador síncrono de comando, consulta, validação e evento de domínio que deve concluir na mesma requisição HTTP. RabbitMQ **não** devolve `Result<T>` ao controller.
- **RabbitMQ** recebe apenas **eventos de integração** já commitados, para efeitos lentos, retentáveis ou horizontais (e-mail SMTP, sync por conta, ingestão pós-commit, jobs de IA/PDF, lotes externos).

### Outbox transacional e dois bancos

- Na **mesma transação** do agregado, a aplicação grava uma linha de outbox. Um **dispatcher** (fora do request HTTP) publica no broker depois do commit.
- É **proibido** chamar o broker dentro do handler HTTP antes de `CommitAsync`.
- **`MasterDb`:** eventos de catálogo (provisionamento, e-mails transacionais de plataforma, cobrança, trial).
- **Banco do tenant:** eventos operacionais (métricas, hierarquia, publicação enfileirada, relatório, IA).
- O envelope operacional carrega **`TenantId`**. O consumidor restaura `ITenantContextAccessor` antes de abrir qualquer `DbContext`. Evento operacional sem `TenantId` vai para **dead-letter**.

### Infraestrutura e limites de referência

- Contratos (`IIntegrationEvent`, gravador de outbox, política de ack a partir de `Result`) residem em `BuildingBlocks.Application` / `BuildingBlocks.Infrastructure` **sem** pacote de broker.
- Transporte RabbitMQ + MassTransit fica no projeto **`BuildingBlocks.Messaging`**, referenciado por `WebApi` e testes.
- Projetos **`Domain`**, **`Application`** (de módulos), **`WebApp`** e **`BackofficeApp`** **não** referenciam MassTransit nem `RabbitMQ.Client` (compliance testável na Fase 2).

### Consumidor e política de ack

- O consumidor **traduz** a mensagem no comando MediatR já existente; não reimplementa regra de negócio.
- **`ErrorType.Validation`, `NotFound`, `Conflict`, `Unauthorized`:** ack; sem retry; log/auditoria.
- **Falha transitória explícita** (429, timeout, SMTP temporário): reagenda na **mesma linha** de outbox (`Pending` + `NextAttemptAtUtc` + `AttemptCount`; índice único em `EventId` impede duplicata).
- **Exceção inesperada:** retry curto no transporte; esgotado → dead-letter.
- **`EventId` já na inbox:** ack idempotente (reentrega).

### Agendamento, escala e papéis de host

- **Agendamento** permanece no SQL (`NextAttemptAtUtc`, laços de background). A v1 **não** usa `rabbitmq_delayed_message_exchange`.
- **Schedulers** que varrem tenants/outbox exigem **lease** no `MasterDb` (espírito do claim do ADR 0046). Até multi-instância operacional (Fase 10), produção de mensageria em instância única com papel `All` (`Messaging__HostRole`).
- Consumidores podem ter **N** réplicas.

### Segurança de carga, kill switches e frontend

- **Carga útil mínima:** identificadores, tipo, correlação, instante. **Sem** token OAuth, connection string, senha SMTP ou corpo de e-mail renderizado; o consumidor relê segredos no cofre do tenant ou nas opções do host ([ADR 0018](0018-dotenv-configuration-and-secrets-management.md)).
- Dispatcher avalia **`killswitch.messaging.dispatch`** ([ADR 0015](0015-feature-flags-and-operational-kill-switches.md)) antes de publicar. Flags existentes (`killswitch.social-publishing.dispatch`, `Integrations:MetaSynchronization:Enabled`, etc.) continuam **dentro** do comando executado pelo consumidor.
- **`WebApp` / `BackofficeApp`** consomem apenas a Web API por `HttpClient` ([ADR 0020](0020-zero-direct-db-access-frontend-api-client.md)). Trabalho longo retorna id de job no `Result<T>`; status por `GET`. Sem cliente AMQP no browser ou SignalR.

### Publicação orgânica (ADR 0046)

- **`PublicationJob`** permanece a **fonte da verdade** do horário e da idempotência de slot.
- A fila **não** substitui o agendador: o worker enfileira apenas jobs **já devidos**; o consumidor invoca o comando de disparo existente.

### Topologia alvo (referência)

- Vhost `admetricspro`, filas **quorum**, exchange topic `admetricspro.integration`, dead-letter dedicado, routing key `{contexto}.{evento}.v{n}`.
- Detalhes operacionais e fases de migração: [roadmap RabbitMQ](../roadmaps/rabbitmq-messaging-roadmap.md) e [módulo messaging](../modules/messaging.md).

## Consequências

### Positivas

- Elimina o buraco **commit → publish in-memory** para fluxos migrados (e-mail, métricas, sync, jobs).
- Permite **escalar consumidores** sem duplicar trabalho externo quando o lease e os papéis de host estiverem ativos.
- Mantém **regras de negócio** nos handlers MediatR existentes e o padrão **`Result<T>`** ([ADR 0004](0004-result-pattern-and-typed-errors.md)).
- **`PublicationJob`** e `TenantNotificationLog` continuam como idempotência de negócio; a inbox futura cobre idempotência de transporte.

### Negativas / Mitigações

- **Complexidade operacional** (RabbitMQ, credenciais, dead-letter, runbooks) — mitigado por Compose ([ADR 0038](0038-production-docker-orchestration.md)), feature flag `Messaging__Enabled` e kill switch.
- **Risco de processamento duplo** — mitigado por inbox, `EventId`, chaves existentes (`CampaignMetric`, `PublicationJob`, `TenantNotificationLog`).
- **Tenant errado no consumidor** — mitigado por `TenantId` obrigatório e dead-letter antes de abrir `DbContext`.
- **MassTransit vazando para domínio/UI** — mitigado por projeto de borda e teste de compliance arquitetural ([ADR 0017](0017-compliance-architecture-and-quality-gates.md)).
- **Custo e latência** do broker — mitigado por cargas mínimas e uso da fila só onde HTTP síncrono é inadequado.