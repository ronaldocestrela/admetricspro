# Especificação de Módulo: Object Storage (MinIO / `IObjectStore`)

Contrato e diagnóstico do armazenamento de objetos gerados pelo AdMetricsPro. A decisão arquitetural está no [ADR 0053](../adr/0053-minio-object-storage.md). A ordem de implementação está no [roadmap MinIO](../roadmaps/minio-object-storage-roadmap.md).

**sem endpoint HTTP de negócio nesta fase**

Não existem rotas versionadas de produto nem `[EndpointSummary]` de mídia/PDF/marca ligados ao storage. A Web API registra `AddObjectStorage` (Fase 3), mas **nenhum handler de módulo** grava ou lê objetos até a **Fase 4**. O endpoint público de stream de mídia (`GET /api/v1/public/media/{token}`) é entrega da **Fase 4** do roadmap.

### Fase 1 — cluster no ambiente (entregue)

- Serviços `minio` e `minio-init` em `docker-compose.yml` / `docker-compose.prod.yml`: três buckets privados (`admetricspro-organic`, `admetricspro-reports`, `admetricspro-branding`), regra ILM de 24 h no prefixo `organic-media/staging/` do bucket orgânico (script idempotente em `deploy/minio/init-buckets.sh`).
- Dev: API S3 e console em `127.0.0.1:9000` e `127.0.0.1:9001`. Produção: **nenhuma** porta MinIO publicada no host ([guia Docker produção](../devops/docker-production.md)).
- Health check nomeado `minio` em `GET /health` somente quando `ObjectStorage__Provider=MinIO`; com `Local` (padrão), o agregado permanece como antes.
- Nenhum handler de módulo grava ou lê objetos no cluster até a Fase 4.

### Fase 2 — porta sem rede (entregue)

- **`BuildingBlocks.Application.Storage`:** `IObjectStore`, `ObjectKey.Create`, `StorageErrors` (códigos estáveis do catálogo da seção 3).
- **Regra de chave:** validação pura por `Guid tenantId` — chaves orgânicas `organic-media/staging|durable/{tenant:N}/{workspace:N}/{asset:N}` ou prefixo `{tenant:N}/…` com ao menos um segmento após o tenant; rejeição de `\`, `..` e segmentos vazios.
- **Fake:** `InMemoryObjectStore` apenas em testes unitários (`UnitTests.Backend.ObjectStorage`).
- **Compliance:** `AgentsArchitectureComplianceTests.ObjectStorageSdk_MustNotBeReferencedOutsideBuildingBlocksInfrastructure` — `AWSSDK.S3` / `AWSSDK.Core` removidos de `SocialPublishing.Infrastructure` (`S3OrganicMediaStore` eliminado); `SocialPublishing:Media:Provider` aceita somente `Local` até a Fase 4.

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

### 1.2 PDF de relatório white-label — `varbinary(max)` no banco do tenant

`GeneratedReport.PdfContent` guarda o PDF compilado na mesma linha de `RenderModelJson`. Download e anexo de e-mail leem essa coluna. O [ADR 0037](../adr/0037-white-label-automated-reports-generator.md) previa mitigar volume via JSON; o binário foi persistido. O [ADR 0053](../adr/0053-minio-object-storage.md) define migração do byte para objeto.

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
| PDF de `GeneratedReport` | `admetricspro-reports` | `{tenantId:N}/{workspaceId:N}/{reportId:N}.pdf` | `PdfObjectKey` (futuro); `PdfContent` sai após cópia | `RenderModelJson` |
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

---

## 6. Próximas entregas documentadas

| Fase | Documentação adicional |
| :--- | :--- |
| 4 | `GET /api/v1/public/media/{token}` em [social-publishing.md](social-publishing.md). |
| 6 | Download/anexo de PDF em [reports-generator.md](reports-generator.md). |
| 7 | Upload de logo em [tenants-white-label.md](tenants-white-label.md). |