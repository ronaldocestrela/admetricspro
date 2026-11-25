# Especificação de Módulo: Object Storage (MinIO / `IObjectStore`)

Contrato e diagnóstico do armazenamento de objetos gerados pelo AdMetricsPro. A decisão arquitetural está no [ADR 0053](../adr/0053-minio-object-storage.md). A ordem de implementação está no [roadmap MinIO](../roadmaps/minio-object-storage-roadmap.md).

**sem endpoint HTTP nesta fase**

Nesta fase (Fase 0) não existem rotas versionadas, `[EndpointSummary]`, payloads JSON de API nem implementação de `IObjectStore` em produção. O endpoint público de stream de mídia (`GET /api/v1/public/media/{token}`) é entrega da **Fase 4** do roadmap, não deste documento inicial.

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

Hoje o padrão é `LocalOrganicMediaStore` (volume da API). Existe `S3OrganicMediaStore` no módulo social, mas Compose não sobe MinIO. Duas réplicas da Web API não compartilham o mesmo disco; backup do volume não acompanha o banco do tenant.

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

`Storage.KeyInvalid` é defesa da implementação: a chave montada pelo módulo inclui o tenant do contexto. `OpenRead` cujo primeiro segmento não é esse tenant retorna falha sem chamar o SDK.

---

## 4. Regras de chave (contrato futuro)

Antes de qualquer chamada ao provedor, a implementação de `IObjectStore` (Fase 2–3) deve rejeitar:

- chave nula ou em branco;
- segmentos `..` ou barra invertida `\`;
- chave cujo prefixo de tenant não coincide com o `TenantId` do `ITenantContextAccessor`.

Chaves orgânicas já persistidas no formato `organic-media/durable/{tenant}/{workspace}/{asset}` permanecem válidas dentro do bucket `admetricspro-organic`.

A interface prevista em `BuildingBlocks.Application.Storage` (Fase 2): `PutAsync`, `OpenReadAsync`, `DeleteAsync`, `ExistsAsync` — argumentos bucket, chave, stream, content type, `CancellationToken`; retorno `Result` ou `Result<Stream>`; sem tipos AWS nas assinaturas públicas.

---

## 5. Configuração (referência, sem implementação nesta fase)

| Variável | Papel |
| :--- | :--- |
| `ObjectStorage__Provider` | `Local` (padrão) ou `MinIO`. |
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
| 2–3 | Contratos C# e exemplos de uso de `IObjectStore` neste arquivo. |
| 4 | `GET /api/v1/public/media/{token}` em [social-publishing.md](social-publishing.md). |
| 6 | Download/anexo de PDF em [reports-generator.md](reports-generator.md). |
| 7 | Upload de logo em [tenants-white-label.md](tenants-white-label.md). |