# Especificação de Módulo: Object Storage (MinIO / `IObjectStore`)

Contrato e diagnóstico do armazenamento de objetos gerados pelo AdMetricsPro. A decisão arquitetural está no [ADR 0053](../adr/0053-minio-object-storage.md). A ordem de implementação está no [roadmap MinIO](../roadmaps/minio-object-storage-roadmap.md).

### Fase 5 — cópia do volume orgânico (entregue)

- **Sem endpoint HTTP:** o operador sobe um processo único da Web API com `ObjectStorage:Provider=MinIO` e `ObjectStorage:MigrateLocalRoot=true` (`OrganicMediaVolumeMigrationHostedService`); o host encerra após o relatório.
- **Origem:** `SocialPublishing:Media:LocalRoot` (volume legado `social-media-data`). **Destino:** bucket `ObjectStorage:OrganicBucket` com a **mesma** chave. Nenhum `UPDATE` em `OrganicMediaAssets`.
- **`OrganicMediaVolumeCopier`:** enumera arquivos, consulta `IObjectStore.GetContentLengthAsync`, ignora quando o tamanho remoto coincide; MIME do `Put` vem do catálogo SQL quando disponível.
- **Relatório:** `Copied`, `Skipped`, `Failed`, `MissingOnDisk` (chave no SQL sem arquivo no volume). Logs registram só a chave.
- **Rollback:** manter o volume montado e `ObjectStorage:Provider=Local` até validar contagens; apagar o volume fica fora desta fase.
- Runbook operacional: [docker-production.md](../devops/docker-production.md) (seção MinIO).

### Fase 4 — mídia orgânica (entregue)

- **`ObjectStoreOrganicMediaStore`:** quando `ObjectStorage:Provider=MinIO`, `IOrganicMediaStore` grava no bucket `ObjectStorage:OrganicBucket` via `IObjectStore`; com `Local`, permanece `LocalOrganicMediaStore` em `SocialPublishing:Media:LocalRoot` (padrão do repositório até concluir a Fase 5 em ambientes legados).
- **Chave inalterada:** `organic-media/staging|durable/{tenant:N}/{workspace:N}/{asset:N}` (`OrganicMediaObjectKeyFormatter`).
- **URL pública:** `SignedOrganicMediaPublicUrlResolver` emite `https://{ObjectStorage:PublicBaseUrl}/api/v1/public/media/{token}` com HMAC (`ObjectStorage:PublishUrlSigningKey`, TTL `ObjectStorage:PublishUrlTtlMinutes`, teto 1440). Sem `PublicBaseUrl`, publicação falha com `Social.MediaPublicBaseUrlMissing`.
- **Endpoint:** `GET /api/v1/public/media/{token}` (`[AllowAnonymous]`) — stream binário com `Content-Type` do token; 404 opaco para token inválido/expirado; 422 para `Storage.KeyInvalid` pós-HMAC.

### Fase 1 — cluster no ambiente (entregue)

- Serviços `minio` e `minio-init` em `docker-compose.yml` / `docker-compose.prod.yml`: três buckets privados (`admetricspro-organic`, `admetricspro-reports`, `admetricspro-branding`), regra ILM de 24 h no prefixo `organic-media/staging/` do bucket orgânico (script idempotente em `deploy/minio/init-buckets.sh`).
- Dev: API S3 e console em `127.0.0.1:9000` e `127.0.0.1:9001`. Produção: **nenhuma** porta MinIO publicada no host ([guia Docker produção](../devops/docker-production.md)).
- Health check nomeado `minio` em `GET /health` somente quando `ObjectStorage__Provider=MinIO`; com `Local` (padrão), o agregado permanece como antes.
- Handlers de mídia orgânica leem/gravam no cluster somente com `ObjectStorage:Provider=MinIO` (Fase 4).

### Fase 2 — porta sem rede (entregue)

- **`BuildingBlocks.Application.Storage`:** `IObjectStore` (`PutAsync`, `OpenReadAsync`, `DeleteAsync`, `ExistsAsync`, `GetContentLengthAsync`), `ObjectKey.Create`, `StorageErrors` (códigos estáveis do catálogo da seção 3).
- **Regra de chave:** validação pura por `Guid tenantId` — chaves orgânicas `organic-media/staging|durable/{tenant:N}/{workspace:N}/{asset:N}` ou prefixo `{tenant:N}/…` com ao menos um segmento após o tenant; rejeição de `\`, `..` e segmentos vazios.
- **Fake:** `InMemoryObjectStore` apenas em testes unitários (`UnitTests.Backend.ObjectStorage`).
- **Compliance:** `AgentsArchitectureComplianceTests.ObjectStorageSdk_MustNotBeReferencedOutsideBuildingBlocksInfrastructure` — `AWSSDK.S3` / `AWSSDK.Core` removidos de `SocialPublishing.Infrastructure` (`S3OrganicMediaStore` eliminado); provedor de mídia orgânica é `ObjectStorage:Provider` (Fase 4).

### Fase 3 — cliente MinIO (entregue)

- **`BuildingBlocks.Infrastructure.Storage`:** `LocalObjectStore`, `MinioObjectStore`, `ObjectStorageOptions`, `AddObjectStorage`.
- **`ObjectStorage:Provider`:** `Local` (padrão) registra só disco em `ObjectStorage:LocalRoot` (padrão `./data/object-storage`, irmão de `SocialPublishing:Media:LocalRoot`); `MinIO` registra `IAmazonS3` singleton (`ForcePathStyle`, credenciais da seção `ObjectStorage`) e `MinioObjectStore`.
- Validação de chave via `ObjectKey.Create` + `ITenantContextAccessor` antes de IO ou chamada S3; falhas estáveis (`StorageErrors`); detalhe do SDK só em log.
- **`AWSSDK.S3` 3.7.410.12** apenas em `BuildingBlocks.Infrastructure`.
- Testes: unitários em `UnitTests.Backend.ObjectStorage` (sem rede); integração `MinioObjectStoreIntegrationTests` + `MinioFixture` (`Testcontainers.Minio`) — única suíte que sobe container MinIO.

---

## 1. Diagnóstico do que existe hoje

Há quatro lugares onde um arquivo pode existir. Só dois guardam binário nosso. Os outros dois não devem ir para o MinIO.

### 1.1 Mídia orgânica (JPEG, PNG, MP4) — disco local, com cliente S3 pronto e desligado

O módulo Social Publishing já separa metadados de binário. A tabela `social.OrganicMediaAssets` guarda `ObjectKey`, MIME, dimensões, duração e proporção. Não há coluna de bytes.

A porta é `IOrganicMediaStore` (`SaveAsync`, `OpenReadAsync`, `DeleteAsync`), com dois tiers:

| Tier | Chave |
| :--- | :--- |
| Staging | `organic-media/staging/{tenantId:N}/{workspaceId:N}/{assetId:N}` |
| Durable | `organic-media/durable/{tenantId:N}/{workspaceId:N}/{assetId:N}` |

Hoje o padrão é `LocalOrganicMediaStore` (volume da API). O cliente S3 que existia no módulo social foi removido na Fase 2; o bucket compartilhado via `IObjectStore` entra na Fase 4. Duas réplicas da Web API não compartilham o mesmo disco; backup do volume não acompanha o banco do tenant.

### 1.2 PDF de relatório white-label — objeto + coluna legada (Fase 6)

Novos relatórios gravam o PDF no bucket `admetricspro-reports` com chave `{tenantId:N}/{workspaceId:N}/{reportId:N}.pdf` e persistem `GeneratedReport.PdfObjectKey`. `RenderModelJson` permanece no SQL. A coluna `PdfContent` (`varbinary`) ainda existe para migração: leitura prioriza o objeto quando a chave está preenchida; download e anexo de e-mail usam `IReportPdfStore` via `ReportPdfContentReader`. Detalhes: [reports-generator.md](reports-generator.md).

### 1.3 Marca white-label — URL colada, não arquivo

`LightLogoUrl`, `DarkLogoUrl` e `FaviconUrl` são strings HTTPS. A agência pode hospedar em CDN externo. Estáticos padrão do produto ficam no `wwwroot` do Blazor.

### 1.4 O que passa pela API e não é arquivo nosso

Upload pago para a Graph API, previews de CDN de anúncios, Creative Hub com URLs de terceiros. O frontend (`WebApp`, `BackofficeApp`) fala só com a Web API ([ADR 0020](../adr/0020-zero-direct-db-access-frontend-api-client.md)).

---

## 2. O que entra em cada bucket (alvo)

| Origem | Bucket | Chave | Metadado que permanece no SQL | Fora deste corte |
| :--- | :--- | :--- | :--- | :--- |
| Upload `POST /api/v1/social/media` (JPEG, PNG, MP4) | `admetricspro-organic` | chave atual com `staging` ou `durable` | `OrganicMediaAssets` | — |
| PNG do Nano Banana após normalização | `admetricspro-organic` | mesma chave durable que `ReplaceGeneratedImage` | `OrganicMediaAssets` | — |
| PDF de `GeneratedReport` | `admetricspro-reports` | `{tenantId:N}/{workspaceId:N}/{reportId:N}.pdf` | `PdfObjectKey`; `PdfContent` legado até migração 6.5 | `RenderModelJson` |
| Logo claro, escuro, favicon enviados no formulário de marca | `admetricspro-branding` | `{tenantId:N}/light`, `{tenantId:N}/dark`, `{tenantId:N}/favicon` | chave opcional; URL externa continua válida | SVG/PNG já hospedados pela agência |
| Upload Meta `POST /api/v1/integrations/meta/media` | — | — | hash / video id na conta de anúncios | binário não retido |
| `wwwroot` | — | — | — | permanece no publish do Blazor |

Staging orgânico abandonado: expiração em 24 horas (ILM no MinIO ou sweep local). Durable e PDF saem quando o agregado dono é removido ou quando existir exclusão por prefixo `{tenantId:N}/` (Fase 7).

---

## 3. Catálogo de erros (contrato `Result` / `Result<T>`)

Erros de storage seguem o envelope descrito em [building-blocks.md](building-blocks.md). A propriedade `error.description` exposta ao cliente **não** inclui exceção do SDK, nome de bucket nem endpoint do MinIO.

| Código | Quando |
| :--- | :--- |
| `Storage.Disabled` | Não usado como falha de request. `Provider=Local` é sucesso no disco. |
| `Storage.ObjectNotFound` | Get ou delete de chave ausente quando o chamador esperava o objeto. Delete de chave já ausente no sweep é sucesso. |
| `Storage.WriteFailed` | Put recusado, bucket inexistente, credencial inválida, timeout. |
| `Storage.ReadFailed` | Get falhou por motivo que não é 404. |
| `Storage.DeleteFailed` | Delete falhou depois de o objeto existir. |
| `Storage.KeyInvalid` | Chave vazia, com `..` ou `\`, ou fora do prefixo do tenant atual. |
| `Social.MediaPublicBaseUrlMissing` | Já existe no módulo social. Falha ao publicar quando `PublicBaseUrl` está vazio. |

`Storage.KeyInvalid` é defesa da implementação: `ObjectKey.Create` valida em relação ao tenant informado; a implementação de `IObjectStore` na Fase 3 usará `ITenantContextAccessor` antes de chamar o SDK.

---

## 4. Contrato `IObjectStore` e `ObjectKey`

Namespace: `BuildingBlocks.Application.Storage`.

### `ObjectKey.Create(string? key, Guid tenantId)` → `Result<ObjectKey>`

Rejeita (código `Storage.KeyInvalid`):

- chave nula ou em branco;
- barra invertida `\` ou segmento `..`;
- segmento vazio (barra dupla, barra inicial/final);
- chave orgânica válida em formato mas com tenant diferente no terceiro segmento;
- qualquer outra chave cujo primeiro segmento não seja `{tenantId:N}` ou que tenha só o segmento do tenant.

Aceita:

- `organic-media/staging|durable/{tenantId:N}/{workspaceId:N}/{assetId:N}` (workspace e asset em formato `N`);
- `{tenantId:N}/light`, `{tenantId:N}/{workspaceId:N}/{reportId:N}.pdf`, etc.

### `IObjectStore`

| Método | Retorno |
| :--- | :--- |
| `PutAsync(bucket, key, content, contentType, cancellationToken)` | `Task<Result>` |
| `OpenReadAsync(bucket, key, cancellationToken)` | `Task<Result<Stream>>` |
| `DeleteAsync(bucket, key, cancellationToken)` | `Task<Result>` (delete idempotente se ausente) |
| `ExistsAsync(bucket, key, cancellationToken)` | `Task<Result<bool>>` |

Nenhum tipo `Amazon.*` nas assinaturas públicas.

Exemplo de validação (testes / chamadores):

```csharp
var keyResult = ObjectKey.Create(rawKey, tenantId);
if (keyResult.IsFailure)
    return keyResult.Error; // Storage.KeyInvalid

// Fase 3+: await _objectStore.OpenReadAsync(bucket, keyResult.Value.Value, cancellationToken);
```

---

## 5. Configuração e DI

| Variável | Papel |
| :--- | :--- |
| `ObjectStorage__Provider` | `Local` (padrão) ou `MinIO`. |
| `ObjectStorage__LocalRoot` | Raiz no disco com `Provider=Local` (padrão `./data/object-storage`). |
| `ObjectStorage__ServiceUrl` | Endpoint interno, ex. `http://minio:9000`. |
| `ObjectStorage__AccessKey` / `ObjectStorage__SecretKey` | Credenciais ([ADR 0018](../adr/0018-dotenv-configuration-and-secrets-management.md)). |
| `ObjectStorage__OrganicBucket` | `admetricspro-organic`. |
| `ObjectStorage__ReportsBucket` | `admetricspro-reports`. |
| `ObjectStorage__BrandingBucket` | `admetricspro-branding`. |
| `ObjectStorage__PublicBaseUrl` | Origem HTTPS da Web API para links de publicação assinados. |
| `ObjectStorage__PublishUrlTtlMinutes` | Vida do token de leitura pública (padrão 120, teto 1440). |
| `ObjectStorage__MigrateLocalRoot` | `true` apenas no processo one-shot de cópia do volume orgânico (Fase 5); padrão `false`. |
| `ObjectStorage__MigrateReportPdfs` | `true` apenas no processo one-shot de cópia de `PdfContent` → bucket de relatórios (Fase 6); padrão `false`. |
| `ObjectStorage__PublishUrlSigningKey` | Chave HMAC de tokens públicos (obrigatória com `PublicBaseUrl`). |

### `GET /api/v1/public/media/{token}`

| Status | Corpo |
| :--- | :--- |
| `200` | Stream binário (`Content-Type` do token assinado). |
| `404` | `Result` com `Social.PublicMediaNotFound` (token inválido, expirado, tenant divergente ou objeto ausente). |
| `422` | `Result` com `Storage.KeyInvalid` quando a chave pós-HMAC é malformada. |

Detalhes de upload e prévia: [social-publishing.md](social-publishing.md).

---

## 6. Fase 6 — PDF de relatório (entregue)

- **Porta:** `IReportPdfStore` em `Analytics.Domain.Reports`; implementação `ObjectStoreReportPdfStore` no bucket `ObjectStorage:ReportsBucket`.
- **Write path:** `GenerateReportCommand` e `DispatchReportScheduleCommand` fazem put antes do commit SQL; falha de put retorna `Storage.WriteFailed` sem linha órfã.
- **Leitura:** `GET .../reports/{id}/pdf` e `GET /api/v1/public/reports/{token}/pdf` — objeto primeiro, coluna legada na janela de migração.
- **E-mail:** anexo PDF opcional em `EmailMessage` quando o agendamento usa `Pdf` ou `Both`.
- **Cópia one-shot:** `ObjectStorage:MigrateReportPdfs=true` com `Provider=MinIO` registra `ReportPdfMigrationHostedService` (encerra o host ao terminar). Não roda no startup normal das réplicas HTTP.

## 7. Próxima entrega documentada

| Fase | Documentação adicional |
| :--- | :--- |
| 7 | Upload de logo em [tenants-white-label.md](tenants-white-label.md). |