# 0048 — Tração orgânica, notificação cross-module e Creative Hub

## Status

Aceito em 2026-11-04. Subfase **6.2** implementada (scan persistido, `OrganicTractionDetectedNotification`, upsert no Creative Hub). Aceite de sugestão, rascunho de anúncio e superfície Blazor permanecem nas subfases **6.3** e **6.4**.

## Contexto

A Fase 6 liga posts orgânicos com engajamento anômalo a sugestões de impulsionamento e ao Creative Hub, sem acoplar `SocialPublishing` a `Analytics` ou `Integrations` por referência de projeto (ADR 0043). A detecção pura já vive em `OrganicTractionDetector` (subfase 6.1).

Requisitos da subfase 6.2:

1. Persistir `PaidBoostSuggestion` em status `Open` no banco social do tenant.
2. Publicar evento leve via `SocialPublishing.Contracts` **após** commit bem-sucedido.
3. Registrar mídia orgânica no Creative Hub por fingerprint estável, sem duplicar ativos.
4. Reutilizar o worker de disparo orgânico e o kill switch existente (ADR 0046).

## Decisão

### Notificação e limites de módulo

1. **`OrganicTractionDetectedNotification`** (`INotification`) em `SocialPublishing.Contracts`, referenciável apenas por `Analytics.Application` e `Integrations.Application` (este último na subfase 6.3).

2. **`ScanOrganicTractionCommand`** sem rota HTTP; invocado pelo `SocialPublishingDispatchWorker` após `DispatchDuePublicationsCommand` no mesmo escopo de tenant.

3. **`IPublisher.Publish`** só depois de `ISocialPublishingUnitOfWork.CommitAsync` por hit. Não há outbox MediatR: entrega in-process; idempotência via sugestão `Open` única por post.

4. **`SocialPublishing.Application` não referencia `Analytics.*`.** O handler `LinkOrganicAssetWhenTractionDetected` vive em `Analytics.Application`.

### Fingerprint e Creative Hub

1. **Fingerprint orgânico:** SHA-256 hex da `ObjectKey` da primeira mídia do post. Ausência de chave → notificação com fingerprint nulo; Analytics registra `Analytics.OrganicFingerprintMissing` e não persiste.

2. **Persistência Analytics:** schema `analytics`, tabela `CreativeHubAssets`, índice único `(WorkspaceId, MediaFingerprint)`. Upsert atualiza métricas e metadados orgânicos na mesma linha.

3. **Leitura existente do hub** (fadiga, overview de anúncios pagos) **não muda** nesta subfase; fingerprints sintéticos de `Ad.PreviewUrl` permanecem separados.

### Migrações

1. **`ISocialPublishingSchemaMigrator`** continua aplicando migrações sociais; **`IAnalyticsSchemaMigrator`** aplica o schema analytics no provisionamento e no hosted service de migração de tenants (mesmo pipeline do decorator em `AddSocialPublishingTenantPersistenceComposition`).

## Consequências

### Positivas

- Contrato estável para 6.3 (`CreatePaidBoostDraftCommand`) sem vazar EF ou domínio social.
- Creative Hub passa a conhecer origem orgânica por fingerprint persistida.
- Operadores desligam disparo **e** scan com um único kill switch.

### Negativas / Mitigações

- Notificação in-process não sobrevive a crash entre commit e publish — mitigação futura: outbox ou reprocessamento idempotente; hoje a sugestão `Open` impede reenvio duplicado na mesma requisição.
- Permalink orgânico ainda nulo no scan — UI e card do hub na subfase 6.4.
- Parágrafo de origem orgânica em `docs/modules/creative-hub-and-fatigue-detector.md` e aceite Integrations → subfases 6.3–6.4.
