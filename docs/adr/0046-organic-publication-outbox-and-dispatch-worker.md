# 0046 — Outbox de publicação orgânica e worker de disparo

## Status

Aceito em 2026-11-03. Subfases **4.2**, **4.3** e **4.4** implementadas (outbox, adaptadores `PublishAsync`, `DispatchDuePublicationsCommand`, `SocialPublishingDispatchWorker` e kill switch `killswitch.social-publishing.dispatch`).

## Contexto

Publicação orgânica deve ocorrer **exatamente uma vez** no horário UTC do slot, com retentativa diante de 429 e capacidade de desligar disparo sem deploy. O roadmap (Fase 4) proíbe fila externa nesta entrega: a durabilidade fica no **SQL Server do tenant**, alinhado ao database-per-tenant (ADR 0002) e ao bounded context isolado (ADR 0043).

Requisitos da outbox:

1. Job durável por slot agendado, com idempotência de insert.
2. Claim exclusivo entre workers ou instâncias concorrentes.
3. Mesma transação lógica que altera `OrganicPost` e abre/cancela jobs (`CommitAsync` único no handler HTTP).
4. Controle de fluxo via `Result<T>` (ADR 0004), sem exceção de negócio.

## Decisão

### Subfase 4.2 (implementada)

1. **Agregado `PublicationJob`** em `SocialPublishing.Domain`, tabela `social.PublicationJobs`, statuses `Pending`, `InProgress`, `RetryScheduled`, `Succeeded`, `DeadLetter`, `Cancelled`.

2. **Chave de idempotência** `{postId:D}:{scheduledAtUtc:O}` (UTC, cultura invariante), índice único. Conflito → `Social.PublicationJobAlreadyExists`.

3. **`RowVersion`** (`IsRowVersion()` no EF) para claim otimista: `TryClaimAsync` marca `InProgress` e chama `SaveChanges`; perdedor recebe `null` via `DbUpdateConcurrencyException`.

4. **HTTP** `POST /api/v1/social/posts/{id}:schedule` (`Approved` → `Scheduled`) abre job `Pending` no mesmo `CommitAsync` que atualiza o post. `:approve` permanece até `Approved` apenas.

5. **`ReschedulePostCommand`**: em post `Scheduled`, cancela job `Pending`/`RetryScheduled`, grava `ScheduleRevision` e abre job com chave nova; job `InProgress` → `Social.PublicationJobInProgress`.

6. **Sem MediatR cross-module** na outbox; repositório scoped no mesmo `SocialPublishingDbContext` do post.

### Subfase 4.3 (implementada)

- `IOrganicPlatformAdapter.PublishAsync` com `OrganicPublishRequest` / `OrganicPublishReceipt`; token decifrado no handler, URLs de mídia resolvidas fora do adaptador.
- Meta Page (feed imagem), Instagram (carrossel + Reel) e TikTok Content Posting (vídeo vertical); HTTP 429 → `PublishRateLimitedError` com `Retry-After` opcional; demais falhas de rede → `Social.PublishFailed`.
- `OrganicPublishRateLimitPolicy` no domínio (sem referência a Integrations): \(T = \min(2^{attempt} \times baseDelay + jitter, maxDelay)\), opções `SocialPublishing:Publish`.

### Subfase 4.4 (implementada)

1. **`DispatchDuePublicationsCommand`** (MediatR, sem rota HTTP): lista jobs devidos (`ListDueAsync`), `TryClaimAsync`, publica via adaptador, `CommitAsync` **por job**. Sucesso: post `Published` + `PublishedExternalId`, job `Succeeded`. Rate limit / falha recuperável: job `RetryScheduled` com `NextAttemptAtUtc = now + max(política, Retry-After)`, post `ReturnToScheduled()`. Teto de tentativas: job `DeadLetter`, post `Failed` via `MarkDispatchExhausted()`.

2. **`SocialPublishingDispatchWorker`** em `WebApi/Background`: opções `SocialPublishing:Dispatch` (`Enabled` padrão **false**, `IntervalSeconds` ≥ 5). Ciclo: avalia `IFeatureFlagService.IsFeatureEnabledAsync("killswitch.social-publishing.dispatch")` **antes** do laço; percorre `GetTenantsQuery` (`Active` / `Trial`); escopo por tenant com `ITenantContextAccessor` e `ISender`. Exceção inesperada em um tenant não impede o seguinte.

3. **Kill switch** semeado no MasterDb (`MasterCatalogFeatureFlagKeys.SocialPublishingDispatch`), ADR 0015.

4. **URLs de mídia**: `IOrganicMediaPublicUrlResolver` + `SocialPublishing:Media:PublicBaseUrl`.

5. **Suposição operacional**: instância única do host **ou** claim `RowVersion` como proteção extra em multi-instância. Job que permanece `InProgress` após cancelamento abrupto do host **não** tem reclaim automático nesta subfase.

## Consequências

### Positivas

- Outbox testável com SQL Server real (claim concorrente) e handlers unitários com repositório em memória.
- Idempotência de slot independente de fila externa ou Integrations.
- Reagendamento auditável: job cancelado permanece na tabela (chave histórica não reutilizada).
- Disparo desligável em produção sem redeploy (kill switch + `Enabled=false` por padrão).

### Negativas / Mitigações

- `TryClaimAsync` faz `SaveChanges` fora do `UnitOfWork` do handler HTTP — aceitável no worker; handlers HTTP usam commit unificado para post+job.
- Multi-instância sem fila exige disciplina de claim — mitigado por `RowVersion`.
- Jobs órfãos em `InProgress` após crash — mitigação futura: job de reclaim ou timeout operacional documentado na spec.
