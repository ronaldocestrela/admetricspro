# 0053 — Armazenamento de objetos com MinIO e fronteira SQL × byte

## Status

Aceito em 2026-11-25.

As **Fases 1–7** do [roadmap de object storage](../roadmaps/minio-object-storage-roadmap.md) ainda não estão implementadas. Até a conclusão dessas fases, `ObjectStorage:Provider` permanece **`Local`** (comportamento atual: volume da API e `GeneratedReport.PdfContent` no SQL do tenant). Este ADR **não** substitui o [ADR 0037](0037-white-label-automated-reports-generator.md); complementa-o quanto ao destino dos bytes do PDF.

## Contexto

O AdMetricsPro persiste arquivos gerados pela plataforma em quatro situações distintas. Apenas duas guardam binário próprio; as outras duas não entram neste corte.

1. **Mídia orgânica (JPEG, PNG, MP4):** O módulo Social Publishing já separa metadados (`social.OrganicMediaAssets.ObjectKey`, MIME, dimensões) do binário. Hoje o padrão é disco local (`LocalOrganicMediaStore`, raiz `./data/social-media`). Existe `S3OrganicMediaStore` com `AWSSDK.S3`, mas Compose e produção não definem MinIO nem `ServiceUrl`; o cliente S3 vive dentro de `SocialPublishing.Infrastructure` e não é reutilizável por outros módulos sem acoplamento.
2. **PDF de relatório white-label:** `GeneratedReport.PdfContent` é `varbinary(max)` no banco operacional do tenant. Download, anexo de e-mail e geração gravam e leem essa coluna. O [ADR 0037](0037-white-label-automated-reports-generator.md) registrou o custo de dezenas de PDFs por tenant; a mitigação escrita (“sintetizar sob demanda a partir do JSON”) **não** foi o caminho implementado — o binário é persistido. Com a Fase 8 da mensageria ([ADR 0052](0052-rabbitmq-integration-events-and-transactional-outbox.md)), a compilação saiu do request HTTP, mas o destino do arquivo continua sendo o SQL.
3. **Marca white-label:** Logos e favicon são URLs HTTPS coladas pela agência; não há upload de arquivo na plataforma neste estado.
4. **Transitório / terceiros:** Upload pago para a Graph API, CDN das redes, estáticos do `wwwroot` do Blazor — binário não retido ou fora do escopo de produto.

Problemas operacionais atuais:

- Duas réplicas da Web API **não** compartilham o mesmo volume de mídia local.
- Backup do volume do container **não** acompanha o backup do banco do tenant.
- Mensagens de falha do store S3 existente podem vazar `ex.Message` do SDK na descrição exposta ao cliente HTTP.

O isolamento [database-per-tenant](0002-database-per-tenant-strategy.md) permanece: metadados e chaves de objeto ficam no SQL do tenant; o cluster de objetos é infraestrutura compartilhada na borda, no mesmo espírito do RabbitMQ ([ADR 0038](0038-production-docker-orchestration.md)).

## Decisão

Introduzir **armazenamento de objetos** via **MinIO** (protocolo S3), com **metadado no SQL** e **bytes no objeto**, mantendo upload e leitura sensível **exclusivamente pela Web API**, sem expor o MinIO ao browser ([ADR 0020](0020-zero-direct-db-access-frontend-api-client.md)).

### Cliente e deployment

- **MinIO** é o deployment alvo; o cliente permanece **`AWSSDK.S3`** com `ForcePathStyle` e `ServiceURL`. **Rejeitado** o pacote NuGet **`Minio`** em paralelo ao SDK da AWS.
- Configuração unificada **`ObjectStorage`** (padrão de variáveis do [ADR 0018](0018-dotenv-configuration-and-secrets-management.md)), em vez de credenciais `AWS_*` soltas dentro do módulo social.

### Topologia de buckets

- **Um cluster MinIO**, três buckets por **finalidade**: `admetricspro-organic`, `admetricspro-reports`, `admetricspro-branding`.
- Toda chave de objeto da plataforma inclui prefixo lógico com **`{tenantId:N}/`** (e, para mídia orgânica, o layout já existente `organic-media/staging|durable/...` dentro do bucket orgânico).
- **Rejeitado bucket por tenant:** o provisionamento já cria banco SQL dedicado; acoplar ciclo de vida do tenant à criação de bucket impede rollback por flag e escala operacional ruim. Bucket dedicado corporativo fica fora deste ADR, no mesmo espírito de instância SQL dedicada (também não padrão).

### Fronteira SQL × objeto

- Valores já gravados em **`OrganicMediaAssets.ObjectKey`** permanecem válidos; migração local → objeto é **cópia com a mesma chave**, sem `UPDATE` em massa na tabela.
- **SQL:** `ObjectKey`, MIME, tamanho, dimensões, `RenderModelJson`, snapshots de marca.
- **MinIO:** bytes do arquivo.
- **Proibida** nova coluna `varbinary` para arquivo. **`RenderModelJson`** permanece no SQL como fonte para recompilar o PDF se o objeto sumir.

### Segurança e rede

- MinIO na rede interna Docker; em produção **sem** publicação de API S3 nem console no host ([ADR 0038](0038-production-docker-orchestration.md)).
- O browser **nunca** recebe access key, secret, URL pré-assinada de upload nem endpoint do MinIO.
- Upload de mídia orgânica permanece **`multipart/form-data`** na Web API (validação de cabeçalho, proporção, RBAC, teto 110 MB). Presigned PUT do browser fica **fora da v1**.
- Credenciais somente em `.env` / `.env.prod` ([ADR 0018](0018-dotenv-configuration-and-secrets-management.md)).

### Entrega pública às redes sociais

- Instagram, Facebook e TikTok fazem GET HTTPS no momento da publicação ([ADR 0046](0046-organic-publication-outbox-and-dispatch-worker.md)). A URL pública **não** aponta para o MinIO.
- `IOrganicMediaPublicUrlResolver` passa a emitir link da **própria API** com token HMAC de vida curta (padrão 120 minutos, teto 1440), rota pública que valida token, tenant e chave antes de fazer stream via `OpenReadAsync`.
- Prévia autenticada no Blazor continua pelo fluxo autenticado da API e ticket curto do circuito; **não** usa o token de publicação.

### Portas e módulos

- **`IObjectStore`** em `BuildingBlocks.Application` (`PutAsync`, `OpenReadAsync`, `DeleteAsync`, `ExistsAsync`; retornos `Result` / `Result<Stream>`; sem tipos AWS nas assinaturas).
- Implementação e **`IAmazonS3`** apenas em **`BuildingBlocks.Infrastructure`**. Projetos `Domain`, `Application` de módulos, `WebApp` e `BackofficeApp` **não** referenciam `AWSSDK.S3` nem `AWSSDK.Core` (gate em testes de compliance, [ADR 0017](0017-compliance-architecture-and-quality-gates.md)).
- **`IOrganicMediaStore`** permanece a porta do Social Publishing (monta chave e tier; chama `IObjectStore` no bucket orgânico).
- Relatórios e branding terão portas finas no respectivo `Application`, implementadas na `Infrastructure` do módulo sobre `IObjectStore`. Analytics e Tenants **não** chamam `IOrganicMediaStore`.

### Provedor e falhas

- **`ObjectStorage:Provider`:** `Local` (padrão) ou `MinIO`. Com `Local`, nenhum código novo abre socket para o cluster. Rollback em produção: voltar a variável enquanto volume local e coluna `PdfContent` ainda existirem.
- Falhas de storage retornam **`Result`** ([ADR 0004](0004-result-pattern-and-typed-errors.md)), códigos estáveis (`Storage.ObjectNotFound`, `Storage.WriteFailed`, `Storage.ReadFailed`, `Storage.DeleteFailed`, `Storage.KeyInvalid`). A **descrição ao cliente** não inclui exceção do SDK, nome de bucket nem endpoint; detalhe só em log estruturado.

### Fora deste ADR

- `wwwroot` do Blazor, URLs de CDN de anúncios, `byte[]` transitório do upload Meta para a Graph API, corpo de e-mail, tokens OAuth, connection strings.
- Copiar criativo de anúncio pago para bucket próprio.
- Upload direto do browser para o bucket.

### Frontend ([ADR 0020](0020-zero-direct-db-access-frontend-api-client.md))

- `WebApp` e `BackofficeApp` **não** ganham cliente S3. Logo enviado pela agência (quando existir upload na Fase 7) volta no DTO como URL da API via `HttpClient`; URL externa colada pela agência continua válida.

### Adendo ao [ADR 0037](0037-white-label-automated-reports-generator.md)

- O gerador vetorial (`WhiteLabelReportPdfGenerator`), portal público, disparo multi-canal e `RenderModelJson` **permanecem**.
- O byte do PDF **deixa de** ser o destino definitivo em `PdfContent`; migra para o bucket `admetricspro-reports` com coluna `PdfObjectKey` no SQL (implementação na Fase 6 do roadmap).
- A coluna `PdfContent` só é removida na **segunda** migração, após cópia verificada — não na mesma entrega que introduz a chave.

### Ordem de construção

Detalhe operacional, fases TDD, Compose, health checks e rotas: [roadmap MinIO](../roadmaps/minio-object-storage-roadmap.md) e [módulo object-storage](../modules/object-storage.md).

## Consequências

### Positivas

- Múltiplas réplicas da Web API passam a ler e gravar o **mesmo** objeto compartilhado.
- Backup e restore de mídia e PDF alinham-se a estratégia de object storage, sem inflar o banco do tenant com `varbinary(max)`.
- Porta **`IObjectStore`** centraliza S3/MinIO e desacopla Social Publishing, Analytics e Tenants da referência ao SDK.
- **`Result<T>`** e fronteira API preservam o modelo de segurança e o isolamento por tenant.
- Flag **`Local` / `MinIO`** permite corte gradual e rollback sem apagar volume no mesmo deploy.

### Negativas / Mitigações

- **Complexidade operacional** (cluster MinIO, init de buckets, ILM de staging, credenciais, health) — mitigado por Compose ([ADR 0038](0038-production-docker-orchestration.md)), `Provider=Local` até fases verdes e runbooks no roadmap.
- **Janela de coexistência** (`Local` + volume, `PdfContent` + `PdfObjectKey`) — mitigado por cópias idempotentes (Fases 5 e 6) e drop de coluna somente após verificação.
- **Latência e dependência** do storage na publicação orgânica e no download de PDF — mitigado por stream na API e tokens de curta duração; mensageria continua com payload mínimo ([ADR 0052](0052-rabbitmq-integration-events-and-transactional-outbox.md)).
- **Risco de vazamento de detalhe de infra** em erros — mitigado por códigos estáveis e compliance de camadas.
- **MinIO exposto por engano** — mitigado por rede interna, buckets privados e proibição de presigned upload no browser.