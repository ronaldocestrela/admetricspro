# Especificação Funcional & Técnica: Social Publishing (AI Social Copilot & Scheduling)

## 1. Visão geral

O bounded context **SocialPublishing** cobre gestão e publicação orgânica na plataforma AdMetricsPro: conexão com Meta (Instagram Professional/Business e Facebook Pages) e TikTok Content Posting API, geração de cronograma e criativos com IA, calendário com aprovação white-label e sugestão de impulsionamento quando um post orgânico ganha tração.

Até a **Subfase 6.3** o módulo inclui scan de tração, sugestões `PaidBoostSuggestion`, aceite/dispensa e handoff `CreatePaidBoostDraftCommand` para rascunho pausado em Integrations. Até a **Subfase 5.4** já incluía o **modelo de domínio puro**, **persistência EF no schema `social`**, `GET /api/v1/social/status`, permissões RBAC `SocialRead` (`social.read`), **`SocialConnectionsManage`** (`social.connections.manage`), **`SocialMediaManage`** (`social.media.manage`) e **`SocialCopilotGenerate`** (`social.copilot.generate`), **state OAuth HMAC-SHA256**, adaptadores com **`ExchangeCodeAsync`** e **`PublishAsync`**, **`OrganicTokenProtector`**, rotas OAuth **`authorize`** / **`callback`**, porta **`IOrganicMediaStore`** (local ou S3-compatível), **`POST /api/v1/social/media`**, **ingestão de Brand Voice**, **planner editorial** e **geração de cópia** (`GenerateSlotCopyCommand` / `GenerateEditorialPlanCommand`), **aprovação interna e agendamento** (`:submit`, `:approve`, `:schedule`, `:reject`, `:reschedule`), **outbox `PublicationJob`**, **`DispatchDuePublicationsCommand`** (somente worker) e **`SocialPublishingDispatchWorker`** com kill switch `killswitch.social-publishing.dispatch` (desligado por padrão), **listagem/revogação local de conexões** (`GET`/`DELETE /api/v1/social/connections`), **UI Blazor** `/social/connections`, **grade de calendário** `/social/calendar` com drag-and-drop de reagendamento, **portal público de aprovação** (`/approve/{token}`), **formulário de voz de marca** `/social/brand-voice` e **geração de cronograma** no calendário — tudo via **`ISocialPublishingClient`** (sem `DbContext`, repositório ou MediatR no frontend). Sinergia orgânica + mídia paga permanece na Fase 6 (ADR 0048).

Stack e limites (detalhados no ADR 0043):

- .NET 10: `SocialPublishing.Domain`, `SocialPublishing.Contracts`, `SocialPublishing.Application`, `SocialPublishing.Infrastructure`, controllers em `WebApi` e consumo via `HttpClient` tipado no `WebApp` (referência apenas a `SocialPublishing.Application`).
- Dados do módulo no SQL Server dedicado do tenant, com `SocialPublishingDbContext` e histórico `social.__EFMigrationsHistory` (implementação na Subfase 1.3).
- Cofre orgânico separado do cofre de mídia paga (ADR 0026); o módulo não referencia `Integrations.*`, `Analytics.*` nem `Tenants.Infrastructure`.
- Comunicação entre módulos via MediatR e tipos em `SocialPublishing.Contracts`.

---

## 2. Máquina de estados do post orgânico

Fluxo principal:

`Draft` → `PendingApproval` → `Approved` → `Scheduled` → `Publishing` → `Published`.

Saídas laterais:

- `Rejected` e `ChangesRequested` a partir de `PendingApproval`.
- `Failed` a partir de `Publishing`.
- `Cancelled` a partir de `Draft`, `PendingApproval`, `Approved`, `Scheduled` ou `Failed`.

Reagendamento permitido somente em `Draft`, `PendingApproval`, `Approved` e `Scheduled`.

Formatos previstos: `FeedImage`, `FeedCarousel`, `Reel`, `TikTokVideo`.

Pilares de conteúdo: `Authority`, `Sale`, `Connection`, `SocialProof`.

---

## 3. Catálogo de erros de negócio

| Código | Quando |
| :--- | :--- |
| `Social.WorkspaceRequired` | `WorkspaceId` vazio. |
| `Social.ConnectionNotFound` | Conexão orgânica inexistente no workspace. |
| `Social.TokenRevoked` | Conexão revogada ou refresh rejeitado pela rede. |
| `Social.OAuthStateInvalid` | State ausente, expirado, adulterado ou de outro tenant/workspace. |
| `Social.RedirectUriInvalid` | Redirect ausente, não absoluto ou HTTP fora de loopback. |
| `Social.TenantRequired` | Tenant não resolvido ao iniciar OAuth orgânico. |
| `Social.OAuthConfigurationMissing` | AppId / ClientKey da rede orgânica não configurado. |
| `Social.OAuthExchangeFailed` | Troca de código rejeitada ou falha de transporte com o provedor. |
| `Social.VaultPurposeMismatch` | Propósito criptográfico não corresponde ao envelope do token. |
| `Social.OrganicAccountNotFound` | Nenhuma conta elegível para publicação após o consentimento. |
| `Social.OrganicAccountAmbiguous` | Mais de uma conta elegível (seleção automática não permitida). |
| `Social.PlatformUnsupported` | Rede fora de `Instagram`, `FacebookPage`, `TikTok`. |
| `Social.MediaAspectInvalid` | Mídia fora da proporção exigida pelo formato. |
| `Social.MediaMimeInvalid` | MIME fora de `image/jpeg`, `image/png`, `video/mp4` ou incompatível com o formato. |
| `Social.MediaUnreadable` | Cabeçalho do arquivo não permite ler dimensões/duração. |
| `Social.MediaTooLarge` | Arquivo acima do teto configurado (`SocialPublishing:Media:MaxUploadBytes`). |
| `Social.CarouselItemCountInvalid` | Carrossel com menos de 2 ou mais de 10 itens. |
| `Social.CaptionTooLong` | Legenda acima do teto da rede. |
| `Social.CopyContainsForbiddenTerm` | Legenda gerada contém termo proibido do Brand Voice. |
| `Social.CopyGenerationFailed` | Provedor de IA indisponível, timeout ou resposta inválida. |
| `Social.AiConfigurationMissing` | `SocialAi:BaseUrl`, `Model` ou `ApiKey` ausentes. |
| `Social.HashtagPolicyViolated` | Hashtag duplicada, com espaço ou acima do teto configurado. |
| `Social.PillarWeightsInvalid` | Pesos dos pilares somam valor diferente de 100 ou algum peso é negativo. |
| `Social.BrandVoiceNotFound` | Workspace sem DNA de marca persistido. |
| `Social.PlanRangeInvalid` | Data final do cronograma anterior à data inicial. |
| `Social.PostsPerWeekInvalid` | Posts por semana fora do intervalo 1–7. |
| `Social.TimeZoneInvalid` | Fuso IANA ausente ou desconhecido no planner. |
| `Social.SlotNotReschedulable` | Post em `Publishing`, `Published` ou `Cancelled`. |
| `Social.InvalidTransition` | Transição de status não permitida pela máquina de estados. |
| `Social.ApprovalTokenInvalid` | Link de aprovação indisponível (token inválido, expirado ou revogado). |
| `Social.ApprovalLinkExpirationInvalid` | Expiração do link inválida na emissão autenticada. |
| `Social.ApprovalCommentRequired` | Comentário ausente em solicitação de alterações no portal público. |
| `Social.ApprovalPostSelectionRequired` | Nenhum post selecionado na decisão pública. |
| `Social.ApprovalTokenInvalid` | Link de aprovação indisponível (token inválido, expirado ou revogado). |
| `Social.ApprovalLinkExpirationInvalid` | Expiração do link inválida na emissão. |
| `Social.ApprovalCommentRequired` | Comentário ausente em solicitação de alterações. |
| `Social.ApprovalPostSelectionRequired` | Nenhum post selecionado na decisão pública. |
| `Social.PostNotFound` | Post inexistente ou de outro workspace. |
| `Social.ScheduleInThePast` | Post em `Scheduled` recebeu `scheduledAtUtc` anterior ao instante atual. |
| `Social.ScheduleInstantNotUtc` | Instant de reagendamento ou revisão sem `DateTimeKind.Utc`. |
| `Social.UserRequired` | Operação exige usuário autenticado no tenant. |
| `Social.ApprovalTokenInvalid` | Token ausente, expirado, revogado ou de outro workspace. |
| `Social.PublishRateLimited` | Rede respondeu 429 ou quota esgotada. |
| `Social.PublishFailed` | Falha definitiva após o teto de tentativas ou erro funcional da rede. |
| `Social.PublishFormatUnsupported` | Formato não suportado pelo adaptador da plataforma do post. |
| `Social.BoostAlreadyRequested` | Já existe sugestão aberta para o mesmo post. |
| `Social.BoostSuggestionNotFound` | Sugestão inexistente ou de outro workspace (mesma resposta nos dois casos). |
| `OrganicTraction.InsufficientData` | Menos de três posts com impressões na janela de 28 dias em todas as redes do workspace. |
| `Social.InsightsReadFailed` | Falha HTTP ou corpo inválido ao ler métricas orgânicas na rede. |

Todas as operações futuras devem retornar `Result` / `Result<T>` com esses códigos estáveis, sem `throw` para regra de negócio.

---

## 4. Modelo de domínio (Subfase 1.2)

Implementação em `SocialPublishing.Domain`, sem Entity Framework. Fábricas estáticas retornam `Result` / `Result<T>`; regras de negócio não lançam exceção.

### Agregados

| Tipo | Responsabilidade |
| :--- | :--- |
| `OrganicConnection` | Conexão OAuth orgânica por workspace; tokens apenas como `EncryptedAccessToken` / `EncryptedRefreshToken`; `Revoke` idempotente emite `OrganicConnectionRevoked`. |
| `OrganicPost` | Post agendado com máquina de estados, legenda, mídia (dimensões), `ScheduledAtUtc` e histórico `ScheduleRevision`. |
| `PublicationJob` | Job outbox por slot agendado: status, chave de idempotência, tentativas e claim otimista via `RowVersion`. |
| `BrandVoiceProfile` | DNA de marca: tom, persona, links absolutos, termos proibidos, `EmojiPolicy`, teto de hashtags e pesos dos pilares. |

### Value objects e constantes

| Tipo | Invariantes |
| :--- | :--- |
| `AspectRatio` / `AspectRatioRules` | Proporções nomeadas com tolerância relativa de **1%**: `1:1`, `4:5` (feed/carrossel), `9:16` (Reel/TikTok). Constantes: `AspectRatioRules.Label11`, `Label45`, `Label916`. |
| `HashtagGroup` | Sem duplicata (case insensitive, `#` opcional), sem espaço interno; quantidade ≤ `min(tetoDoPerfil, tetoDaRede)` (Instagram/Page: 30, TikTok: 8). |
| `PillarWeights` | Quatro inteiros ≥ 0 somando exatamente 100. |
| `CaptionLimits` | Instagram, Facebook Page e TikTok: **2200** caracteres (constantes nomeadas, revisáveis). |
| `EditorialSlot` | Slot planejado: `LocalDate`, `UtcInstant`, pilar, rede e formato; sem legenda. |
| `EditorialCalendarPlanner` | Serviço estático de domínio; entrada intervalo, fuso IANA, posts/semana e semente; usa pesos de `BrandVoiceProfile`. |

### Transições de `OrganicPost`

| De | Para (permitido) | Rota HTTP autenticada (Subfase 4.1) |
| :--- | :--- | :--- |
| `Draft` | `PendingApproval`, `Cancelled` | `POST /api/v1/social/posts/{id}:submit?workspaceId=` |
| `PendingApproval` | `Approved`, `Rejected`, `ChangesRequested`, `Cancelled` | `:approve`, `:reject` |
| `Approved` | `Scheduled`, `Cancelled` | `POST /api/v1/social/posts/{id}:schedule?workspaceId=` |
| `Scheduled` | `Publishing`, `Cancelled` | — |
| `Publishing` | `Published`, `Failed` | — |
| `Failed` | `Cancelled` | — |

Reagendamento (`Reschedule`): permitido em `Draft`, `PendingApproval`, `Approved`, `Scheduled`. Entrada HTTP: **`scheduledAtUtc`** (UTC). Cada alteração grava `ScheduleRevision` (de/para, `revisedByUserId`, `revisedAtUtc`). Post em `Scheduled` não aceita instante anterior ao relógio da aplicação → `Social.ScheduleInThePast`. Bloqueado em `Publishing`, `Published`, `Cancelled` → `Social.SlotNotReschedulable`. Permissão: `social.copilot.generate` (`SocialCopilotGenerate`).

Carrossel (`FeedCarousel`): entre **2** e **10** itens de mídia.

### Exemplo JSON — post em `Draft`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "workspaceId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "connectionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "platform": "Instagram",
  "format": "FeedImage",
  "pillar": "Authority",
  "status": "Draft",
  "caption": "Legenda dentro do teto da rede.",
  "scheduledAtUtc": "2026-06-02T13:00:00Z",
  "media": [
    {
      "width": 1080,
      "height": 1350,
      "sortOrder": 0
    }
  ]
}
```

---

## 5. Persistência no banco do tenant (Subfase 1.3)

Contexto: `SocialPublishingDbContext` (não herda `TenantDbContext`). Histórico EF: `social.__EFMigrationsHistory`. Repositórios: `IOrganicConnectionRepository`, `IOrganicPostRepository`, `IBrandVoiceRepository`; commit transacional: `ISocialPublishingUnitOfWork` (publica eventos de domínio via MediatR após `SaveChanges`).

### Diagrama lógico (schema `social`)

| Tabela | Colunas principais | Índices / FK |
| :--- | :--- | :--- |
| `OrganicConnections` | `Id`, `WorkspaceId`, `Platform`, `ExternalAccountId`, `DisplayName`, `EncryptedAccessToken`, `EncryptedRefreshToken`, `AccessTokenExpiresAtUtc`, `Scopes`, `Status`, `CreatedAtUtc` | Único `(WorkspaceId, Platform, ExternalAccountId)` |
| `OrganicPosts` | `Id`, `WorkspaceId`, `ConnectionId`, `Platform`, `Format`, `Pillar`, `Caption`, `ScheduledAtUtc`, `Status`, `CreatedAtUtc` | `(WorkspaceId, ScheduledAtUtc)` |
| `ScheduleRevisions` | `Id`, `OrganicPostId`, `PreviousScheduledAtUtc`, `NewScheduledAtUtc`, `RevisedByUserId`, `RevisedAtUtc` | FK → `OrganicPosts` (cascade) |
| `OrganicMediaAssets` | `Id`, `OrganicPostId` (nullable), `WorkspaceId`, `Format`, `ObjectKey`, `MimeType`, `DurationMilliseconds`, `AspectLabel`, `Width`, `Height`, `SortOrder` | FK opcional → `OrganicPosts` (cascade); índice `WorkspaceId`. **Sem coluna de binário.** |
| `BrandVoiceProfiles` | `Id`, `WorkspaceId`, `Tone`, `Persona`, `EmojiPolicy`, `HashtagLimit`, pilares (`PillarAuthority`…), `LanguageCode`, `MandatoryLinksJson`, `ForbiddenTermsJson`, `CreatedAtUtc` | Único `WorkspaceId` |

Exemplo de valor persistido para token (nunca plaintext):

```json
{
  "encryptedAccessToken": "AES256-CBC-BASE64-CIPHERTEXT-EXAMPLE",
  "encryptedRefreshToken": null
}
```

---

## 6. HTTP — resumo do módulo (Subfase 1.4)

### `GET /api/v1/social/status`

- **Autenticação:** esquema `Tenant` (JWT) + tenant resolvido (`X-Tenant-Id` ou claim `tenant_id`).
- **Autorização:** `TenantPermission.SocialRead` (`social.read`), com validação de carteira quando o papel exige.
- **Query:** `workspaceId` (GUID obrigatório).

#### Exemplo de resposta (`200 OK`)

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": {
    "connections": 0,
    "scheduledPosts": 0
  }
}
```

| Código | HTTP | Quando |
| :--- | :---: | :--- |
| `Auth.Unauthorized` | 401 | JWT ausente ou inválido |
| `Auth.Forbidden` | 403 | Usuário sem `SocialRead` ou fora da carteira |
| `Social.WorkspaceRequired` | 400 | `workspaceId` vazio |
| `Workspace.NotFound` | 404 | Workspace inexistente no inquilino (consulta Tenants na composição) |

---

## 6.1 OAuth orgânico — início do fluxo (Subfase 2.1 / rota 2.2)

Consulta MediatR **`StartOrganicAuthorizationQuery`** (Application), exposta via **`POST /api/v1/social/connections/authorize`** com permissão **`social.connections.manage`**.

### Entrada (query)

| Campo | Tipo | Regra |
| :--- | :--- | :--- |
| `WorkspaceId` | GUID | Obrigatório |
| `Platform` | `Instagram` \| `FacebookPage` \| `TikTok` | Enum suportado |
| `RedirectUri` | string | Absoluta; HTTPS ou HTTP em loopback |

### State anti-CSRF

- Formato: `{base64url(json)}.{base64url(hmac-sha256)}`.
- Payload: `TenantId`, `WorkspaceId`, `OrganicPlatform`, `RedirectUri`, `IssuedAtUtc`, `Nonce` (32 bytes hex).
- TTL: **15 minutos**; mesma resposta `Social.OAuthStateInvalid` para adulteração, expiração ou tenant/workspace divergente.
- Chave HMAC: `SocialPublishing:OAuth:StateSigningKey` (**distinta** de `Integrations:OAuth:StateSigningKey`).

### Provedores e escopos default (`OrganicOAuthOptions`)

| Plataforma | Host de autorização | Escopos default |
| :--- | :--- | :--- |
| Instagram | `https://www.facebook.com/{GraphApiVersion}/dialog/oauth` | `pages_show_list,pages_read_engagement,instagram_basic,instagram_content_publish` |
| Facebook Page | idem Meta | `pages_show_list,pages_read_engagement,pages_manage_posts` |
| TikTok | `https://www.tiktok.com/v2/auth/authorize/` | `user.info.basic,video.publish,video.upload` |

`META_APP_ID`, `META_APP_SECRET` e `META_GRAPH_API_VERSION` podem alimentar a Meta orgânica; TikTok orgânico usa `SocialPublishing:OAuth:TikTok:ClientKey`.

### Exemplo de retorno (`Result<StartOrganicAuthorizationResponse>`)

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": {
    "authorizationUrl": "https://www.facebook.com/v26.0/dialog/oauth?client_id=...&redirect_uri=...&state=...&scope=...&response_type=code",
    "state": "eyJUZXNhbnRJZCI6...signature",
    "expiresAtUtc": "2026-11-02T15:15:00Z"
  }
}
```

| Código | Quando |
| :--- | :--- |
| `Social.WorkspaceRequired` | `WorkspaceId` vazio |
| `Social.PlatformUnsupported` | Plataforma inválida |
| `Social.RedirectUriInvalid` | Redirect inválido |
| `Social.TenantRequired` | Contexto de tenant ausente |
| `Social.OAuthConfigurationMissing` | Credencial do app da rede não configurada |

---

## 6.2 OAuth orgânico — callback e cofre (Subfase 2.2)

Comando MediatR **`CompleteOrganicAuthorizationCommand`**, rota **`POST /api/v1/social/connections/callback`**, mesma permissão **`social.connections.manage`**.

### Entrada (`CompleteOrganicAuthorizationRequest`)

| Campo | Tipo | Regra |
| :--- | :--- | :--- |
| `workspaceId` | GUID | Obrigatório; deve coincidir com o payload do `state` |
| `code` | string | Obrigatório salvo `providerError` |
| `state` | string | State HMAC emitido no authorize |
| `redirectUri` | string | Deve ser idêntica à usada na emissão do state |
| `providerError` | string? | Se presente → `Social.OAuthExchangeFailed` (sem troca de código) |

### Cofre (`OrganicTokenProtector`)

- Envelope JSON `{ purpose, secret }` cifrado com **`IEncryptionService`** (AES-256-CBC do kernel).
- Propósito: `organic:{platform}:{workspaceId}:{connectionId}` (`OrganicConnection.EncryptionPurpose`).
- Decifrar com propósito divergente → **`Social.VaultPurposeMismatch`** (resposta não contém o segredo).
- A API **nunca** retorna access token ou refresh token.

### Resolução de conta (Meta)

- **Instagram:** exatamente uma Page com `instagram_business_account`; persiste id/username do IG e **token da Page**.
- **Facebook Page:** exatamente uma Page em `/me/accounts`.
- **TikTok:** `open_id` + `display_name` via Content Posting API; refresh token cifrado quando fornecido.

### Reconexão

- Índice único `(WorkspaceId, Platform, ExternalAccountId)`: segunda conclusão **atualiza** a mesma linha (`Reauthorize`), preservando `ConnectionId`.
- Conexão previamente `Revoked` volta a `Active` após novo consentimento.

### Exemplo de sucesso (`200 OK`, sem segredos)

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": {
    "connectionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "platform": "Instagram",
    "displayName": "brand",
    "accessTokenExpiresAtUtc": "2026-12-02T12:00:00Z"
  }
}
```

| Código | HTTP | Quando |
| :--- | :---: | :--- |
| `Social.OAuthStateInvalid` | 422 | State inválido ou `redirectUri` divergente |
| `Social.OAuthExchangeFailed` | 422 | Provedor rejeitou código ou negou consentimento |
| `Social.OrganicAccountNotFound` | 422 | Nenhuma conta publicável encontrada |
| `Social.OrganicAccountAmbiguous` | 422 | Múltiplas contas elegíveis |
| `Social.VaultPurposeMismatch` | 422 | Falha interna de propósito ao decifrar (testes/consumo futuro) |
| `Auth.Forbidden` | 403 | Sem `social.connections.manage` ou fora da carteira |
| `Workspace.NotFound` | 404 | Workspace inexistente no inquilino |

**Fora da Subfase 2.2:** nonce/state de uso único persistido em banco (state continua stateless; o code é single-use no provedor). **Refresh preventivo** e **revogação remota na rede** permanecem na Subfase 2.3 (pendente).

---

## 6.3 OAuth orgânico — listagem e revogação local (Subfase 2.3 parcial / 5.1)

Consulta **`ListOrganicConnectionsQuery`** (`social.read`) e comando **`RevokeOrganicConnectionCommand`** (`social.connections.manage`). A revogação local zera o refresh token e substitui o ciphertext por marcador fixo (`OrganicConnection.RevokedCredentialMarker`); **não** chama a API da Meta/TikTok.

| Rota | Permissão | Descrição |
| :--- | :--- | :--- |
| `GET /api/v1/social/connections?workspaceId=` | `social.read` | Lista metadados sem segredos |
| `DELETE /api/v1/social/connections/{connectionId}?workspaceId=` | `social.connections.manage` | Revoga localmente (idempotente) |

### Exemplo de listagem (`200 OK`)

```json
{
  "isSuccess": true,
  "value": [
    {
      "connectionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "workspaceId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "platform": "Instagram",
      "displayName": "marca",
      "externalAccountId": "17841400000000000",
      "status": "Active",
      "accessTokenExpiresAtUtc": "2026-12-02T12:00:00Z"
    }
  ]
}
```

| Código | HTTP | Quando |
| :--- | :---: | :--- |
| `Social.ConnectionNotFound` | 404 | `connectionId` inexistente ou de outro workspace |
| `Social.TenantRequired` | 401 | Tenant não resolvido no pipeline |
| `Social.WorkspaceRequired` | 400 | `workspaceId` vazio |

---

## 7. Mídia orgânica — metadados e object store (Subfase 2.4)

### Decisão

O SQL Server do tenant guarda **somente metadados** (`ObjectKey`, MIME, dimensões, duração, rótulo de proporção). O binário fica na porta **`IOrganicMediaStore`** (`SaveAsync`, `OpenReadAsync`, `DeleteAsync`). Upload inválido não deixa objeto órfão: gravação em **staging**, validação de domínio, `DeleteAsync` em falha; promoção para **durable** só após sucesso.

### Proporções (fixadas nos testes `OrganicMediaAssetTests`)

| Formato | Proporções aceitas |
| :--- | :--- |
| `FeedImage`, `FeedCarousel` | `1:1`, `4:5` |
| `Reel`, `TikTokVideo` | `9:16` |
| `FeedCarousel` (itens após o primeiro) | Mesmo rótulo da mídia âncora (`carouselAnchorAssetId`) |

### MIME permitidos

- Imagens: `image/jpeg`, `image/png` (feed/carrossel).
- Vídeo: `video/mp4` (Reel/TikTok).

### Porta `IOrganicMediaStore`

| Método | Contrato |
| :--- | :--- |
| `SaveAsync` | Recebe `OrganicMediaStorageTier` (`Staging` ou `Durable`). Chave durável: `{KeyPrefix}/durable/{tenantId}/{workspaceId}/{assetId}`. |
| `OpenReadAsync` | Retorna `Stream` legível para inspeção de cabeçalho ou publicação futura. |
| `DeleteAsync` | Remove objeto por chave (limpeza de staging ou rollback). |

Implementações: **`LocalOrganicMediaStore`** (`SocialPublishing:Media:Provider=Local`, `LocalRoot`) e **`S3OrganicMediaStore`** (`Provider=S3`, `Bucket`, `ServiceUrl`, credenciais via ambiente ADR 0018).

### `POST /api/v1/social/media`

- **Autenticação:** esquema `Tenant` + tenant resolvido.
- **Autorização:** `social.media.manage` (`TenantPermission.SocialMediaManage`).
- **Content-Type:** `multipart/form-data`
- **Campos:** `workspaceId` (GUID), `format` (`FeedImage`…), `file`, `carouselAnchorAssetId` (opcional).
- **Limite:** `SocialPublishing:Media:MaxUploadBytes` (padrão 110 MB); acima → **413**.

**Resposta 201:**

```json
{
  "isSuccess": true,
  "value": {
    "assetId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "workspaceId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "format": "FeedImage",
    "mimeType": "image/jpeg",
    "width": 1080,
    "height": 1080,
    "durationMilliseconds": null,
    "aspectLabel": "1:1",
    "objectKey": "organic-media/durable/{tenantId}/{workspaceId}/{assetId}"
  }
}
```

| Código | HTTP | Quando |
| :--- | :---: | :--- |
| `Social.MediaAspectInvalid` | 422 | Proporção ou carrossel inconsistente |
| `Social.MediaMimeInvalid` | 422 | MIME não permitido ou incompatível |
| `Social.MediaUnreadable` | 422 | Cabeçalho ilegível |
| `Social.MediaTooLarge` | 413 | Arquivo acima do teto |
| `Auth.Forbidden` | 403 | Sem `social.media.manage` |

Comando MediatR: **`RegisterOrganicMediaCommand`** (stream não é persistido; apenas metadados e chave).

---

## 7.1 Brand Voice — ingestão (Subfase 3.1)

Um perfil por workspace (`social.BrandVoiceProfiles`, índice único em `WorkspaceId`). Segunda gravação **atualiza** o mesmo registro (`Id` e `CreatedAtUtc` preservados).

Comandos MediatR: **`UpsertBrandVoiceCommand`**, consulta **`GetBrandVoiceQuery`**.

| Rota | Permissão | Descrição |
| :--- | :--- | :--- |
| `PUT /api/v1/social/brand-voice/{workspaceId}` | `social.copilot.generate` | Cria ou atualiza o DNA de marca |
| `GET /api/v1/social/brand-voice/{workspaceId}` | `social.read` | Lê o perfil persistido |

### Entrada (`UpsertBrandVoiceRequest`)

```json
{
  "tone": "Profissional e acolhedor",
  "persona": "Especialista em performance digital",
  "mandatoryLinks": ["https://cliente.com/sobre"],
  "forbiddenTerms": ["grátis", "milagre"],
  "emojiPolicy": "Sparingly",
  "hashtagLimit": 20,
  "authorityWeight": 40,
  "saleWeight": 20,
  "connectionWeight": 25,
  "socialProofWeight": 15,
  "languageCode": "pt-BR"
}
```

`emojiPolicy`: `Allowed`, `Sparingly`, `Forbidden`.

### Exemplo de sucesso (`200 OK`, `Result<BrandVoiceDto>`)

```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "workspaceId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "tone": "Profissional e acolhedor",
    "persona": "Especialista em performance digital",
    "mandatoryLinks": ["https://cliente.com/sobre"],
    "forbiddenTerms": ["grátis", "milagre"],
    "emojiPolicy": "Sparingly",
    "hashtagLimit": 20,
    "authorityWeight": 40,
    "saleWeight": 20,
    "connectionWeight": 25,
    "socialProofWeight": 15,
    "languageCode": "pt-BR",
    "createdAtUtc": "2026-11-03T12:00:00Z"
  }
}
```

### Erros de borda

| Código | HTTP | Quando |
| :--- | :---: | :--- |
| `Social.WorkspaceRequired` | 400 | `workspaceId` vazio na rota |
| `Social.PillarWeightsInvalid` | 422 | Soma dos quatro pesos ≠ 100 ou peso negativo (repositório **não** é chamado) |
| `Social.BrandVoiceRequired` | 422 | Tom ou persona vazios |
| `Social.HashtagLimitInvalid` | 422 | Teto de hashtags ≤ 0 |
| `Social.MandatoryLinkInvalid` | 422 | Link não absoluto |
| `Social.BrandVoiceNotFound` | 404 | `GET` sem perfil para o workspace |

---

## 7.2 Cronograma editorial determinístico (Subfase 3.2)

Serviço puro em `SocialPublishing.Domain.Planning`: **`EditorialCalendarPlanner.Plan`**. Não há rota HTTP, persistência nem provedor de IA nesta subfase. Os pesos vêm exclusivamente de `BrandVoiceProfile.PillarWeights`.

### Entrada lógica

| Parâmetro | Regra |
| :--- | :--- |
| `brandVoice` | Obrigatório; `null` → `Social.BrandVoiceNotFound` |
| `rangeStart` / `rangeEnd` | `DateOnly` inclusivos; fim &lt; início → `Social.PlanRangeInvalid` |
| `timeZoneId` | IANA (ex.: `America/Sao_Paulo`); vazio ou inválido → `Social.TimeZoneInvalid` |
| `postsPerWeek` | Inteiro **1–7**; fora → `Social.PostsPerWeekInvalid` |
| `seed` | Altera ordem de pilares e deslocamento do ciclo rede/formato; **não** altera contagens por pilar |

Horário local fixo por slot: **10:00** no fuso informado. Conversão via `TimeZoneInfo.ConvertTimeToUtc` (inclui regras históricas de horário de verão).

### Semanas e dias

- Semana editorial começa na **segunda-feira** (calendário civil no fuso do workspace).
- Por semana, dias candidatos na ordem seg → ter → qua → qui → sex → sáb → dom, limitados a `postsPerWeek` e ao intervalo `[rangeStart, rangeEnd]`.
- Semana parcial usa a **contagem real** de slots daquela semana (`N`) para quotas, não `postsPerWeek` quando há menos dias no intervalo.

### Quotas por pilar (maior resto / Hamilton)

Para cada semana com `N` slots:

1. `cota = N × peso / 100` (decimal).
2. Atribui o piso inteiro de cada pilar.
3. Distribui assentos restantes pelos maiores restos fracionários.
4. Empate de resto: `Authority`, `Connection`, `SocialProof`, `Sale`.

Exemplo **N = 5**, pesos **40 / 20 / 25 / 15**: contagens **2 / 1 / 1 / 1** (`Authority` recebe 2; `SocialProof` recebe o assento extra pelo resto 0,75).

### Ordem dos pilares e espaçamento de `Sale`

1. Monta lista na ordem fixa `Authority`, `Sale`, `Connection`, `SocialProof`, repetindo cada pilar conforme a contagem.
2. Rotação à esquerda de `seed % N`.
3. Se dois `Sale` ocupam **datas locais consecutivas** e existe um slot não-`Sale` separável, troca o segundo `Sale` com o não-`Sale` mais próximo à frente (depois para trás). Se não for possível, mantém adjacência.

### Ciclo rede / formato

Índice `(ordinalNaSemana + seed) % 5`, zero-based na semana:

| Índice | Rede | Formato |
| :---: | :--- | :--- |
| 0 | Instagram | FeedImage |
| 1 | FacebookPage | FeedImage |
| 2 | Instagram | Reel |
| 3 | TikTok | TikTokVideo |
| 4 | Instagram | FeedCarousel |

### Exemplo de semana (2026-11-02 a 2026-11-06, 5 posts, 40/20/25/15, seed 0, `America/Sao_Paulo`)

| Dia local | UTC (10:00 local) | Pilar | Rede | Formato |
| :--- | :--- | :--- | :--- | :--- |
| 2026-11-02 (seg) | 2026-11-02T13:00:00Z | Authority | Instagram | FeedImage |
| 2026-11-03 (ter) | 2026-11-03T13:00:00Z | Sale | FacebookPage | FeedImage |
| 2026-11-04 (qua) | 2026-11-04T13:00:00Z | Connection | Instagram | Reel |
| 2026-11-05 (qui) | 2026-11-05T13:00:00Z | SocialProof | TikTok | TikTokVideo |
| 2026-11-06 (sex) | 2026-11-06T13:00:00Z | Authority | Instagram | FeedCarousel |

Prova de fuso (DST histórico): segunda **2018-10-29** 10:00 → **13:00Z**; segunda **2018-11-05** 10:00 → **12:00Z** (UTC−2 na segunda semana).

### Saída (`EditorialSlot`)

Cada item contém `localDate`, `utcInstant`, `pillar`, `platform`, `format`. Sem caption, hashtags ou mídia. Materialização em `OrganicPost` fica na Subfase 3.4.

---

## 7.3 Legendas, copys e hashtags por rede (Subfase 3.3)

Comando MediatR **`GenerateSlotCopyCommand`** (permissão `social.copilot.generate`). Consumido internamente pelo **`POST /api/v1/social/plans:generate`** (Subfase 3.4).

### Fluxo

1. Carrega `BrandVoiceProfile` do workspace (`Social.BrandVoiceNotFound` se ausente).
2. Monta **`CopyBrief`** determinístico (`CopyBriefBuilder` + `CopyFormatInstructions`) — persona, tom, termos proibidos, links, pilar, rede, formato e instrução específica (carrossel → sequência de cards; Reel/TikTok → gancho falado; feed Instagram vs Facebook Page distintos). **Nenhum token OAuth** entra no brief.
3. Chama **`ISocialCreativeGenerator`** (`HttpSocialCreativeGenerator`: `POST {BaseUrl}/chat/completions`, API compatível com OpenAI).
4. **`CopyAcceptanceGate`** valida legenda (`CaptionLimits`, termos proibidos) e hashtags unificadas (`HashtagGroup.Create` com teto `min(perfil, rede)`).
5. Retorna **`GeneratedCopyDto`**; falhas não persistem post.

### Configuração (`SocialAi` / ADR 0018)

| Chave | Descrição |
| :--- | :--- |
| `SocialAi:BaseUrl` | URL base da API (ex.: `https://api.openai.com/v1`) |
| `SocialAi:Model` | Identificador do modelo |
| `SocialAi:ApiKey` | Chave Bearer (ou `SOCIAL_AI_API_KEY`) |
| `SocialAi:Temperature` | Padrão `0.7` |
| `SocialAi:MaxTokens` | Padrão `800` |
| `SocialAi:TimeoutSeconds` | Padrão `60` |

### Entrada do comando (exemplo)

```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "localDate": "2026-11-02",
  "utcInstant": "2026-11-02T13:00:00Z",
  "pillar": "Authority",
  "platform": "Instagram",
  "format": "FeedImage"
}
```

### Brief lógico (campos inspecionáveis em teste)

O texto enviado ao modelo agrega: persona, tom, idioma, pilar, rede, formato, política de emoji, tetos de legenda/hashtag, termos proibidos, links obrigatórios e `formatInstruction`. O modelo deve responder JSON:

```json
{
  "caption": "Texto da legenda…",
  "hashtags": {
    "brand": ["marca"],
    "niche": ["nicho"],
    "reach": ["alcance"]
  }
}
```

### Saída (`Result<GeneratedCopyDto>`)

```json
{
  "isSuccess": true,
  "value": {
    "caption": "Texto validado…",
    "hashtagGroups": {
      "brand": ["marca"],
      "niche": ["nicho"],
      "reach": ["alcance"]
    },
    "network": "Instagram",
    "pillar": "Authority",
    "warnings": ["Link obrigatório ausente na legenda: https://example.com/…"]
  }
}
```

`warnings` não bloqueiam a cópia (link obrigatório ausente). Termo proibido, legenda longa ou hashtag inválida reprovam antes de expor o DTO.

### Erros mapeados

| Código | HTTP (quando exposto na 3.4) | Quando |
| :--- | :---: | :--- |
| `Social.WorkspaceRequired` | 400 | `workspaceId` vazio |
| `Social.BrandVoiceNotFound` | 404 | Sem perfil de marca |
| `Social.CopyContainsForbiddenTerm` | 422 | Termo proibido na legenda |
| `Social.CaptionTooLong` | 422 | Acima de `CaptionLimits` |
| `Social.HashtagPolicyViolated` | 422 | Duplicata, espaço ou teto |
| `Social.CopyGenerationFailed` | 502 (comando isolado) / item em `failedSlots` (`plans:generate`) | Falha do provedor |
| `Social.AiConfigurationMissing` | 422 | Config incompleta |

---

## 7.4 Materializar plano em rascunhos (Subfase 3.4)

Orquestra **`EditorialCalendarPlanner.Plan`**, **`GenerateSlotCopyCommand`** (via `ISender`) e persistência de um **`OrganicPost`** em **`Draft`** por slot cuja cópia foi aceita. Permissão **`social.copilot.generate`**. Leitura do calendário: **`GET /api/v1/social/posts`** com **`social.read`**.

### Rotas

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `POST` | `/api/v1/social/plans:generate` | Materializa o plano (corpo JSON abaixo). |
| `GET` | `/api/v1/social/posts?workspaceId=&from=&to=` | Lista posts no intervalo UTC inclusivo de `ScheduledAtUtc`. |

### Entrada (`GenerateEditorialPlanRequest`)

```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "rangeStart": "2026-11-02",
  "rangeEnd": "2026-11-06",
  "timeZoneId": "America/Sao_Paulo",
  "postsPerWeek": 5,
  "seed": 0
}
```

### Idempotência

Chave lógica: **`workspaceId` + `rangeStart` + `rangeEnd` + `seed`**. Persistida em `social.EditorialPlanGenerations` (índice único). Reexecução com a mesma chave devolve o **`GeneratedPlanDto`** já gravado, sem novas chamadas de IA nem posts duplicados. `postsPerWeek` e `timeZoneId` **não** entram na chave.

### Fluxo por slot

1. Resolve conexão **`Active`** mais recente da plataforma do slot (`Social.ConnectionRequired` em `failedSlots` se ausente).
2. Gera cópia via **`GenerateSlotCopyCommand`**.
3. Compõe legenda persistida: texto aceito + linha em branco + hashtags `#tag` (validação final contra `CaptionLimits`).
4. Cria post com **`OrganicPost.CreateEditorialDraft`** (Reel / carrossel / TikTok **sem mídia** nesta subfase).
5. **`CommitAsync` próprio por slot** (sucesso ou falha registrada no snapshot).

### Saída (`Result<GeneratedPlanDto>`) — sucesso parcial = HTTP 200

```json
{
  "isSuccess": true,
  "value": {
    "createdPostIds": ["a1b2c3d4-e5f6-7890-abcd-ef1234567890"],
    "failedSlots": [
      {
        "localDate": "2026-11-05",
        "utcInstant": "2026-11-05T13:00:00Z",
        "pillar": "SocialProof",
        "platform": "TikTok",
        "format": "TikTokVideo",
        "errorCode": "Social.CopyGenerationFailed",
        "errorDescription": "Não foi possível gerar a cópia com o provedor de IA. Tente novamente."
      }
    ]
  }
}
```

### Casos de borda

| Cenário | HTTP | Comportamento |
| :--- | :---: | :--- |
| Brand Voice ausente | **404** | Nenhum post criado; nenhum `CommitAsync` de slot. |
| Provedor indisponível no meio do plano | **200** | Slots anteriores permanecem; falha listada em `failedSlots`. |
| Chave de idempotência repetida | **200** | Mesmo `createdPostIds` / `failedSlots` do snapshot; sem duplicar. |
| Geração concorrente mesma chave (registro `InProgress`) | **409** | `Social.PlanGenerationInProgress`. |
| `to` &lt; `from` no GET | **422** | `Social.PostsRangeInvalid`. |

---

## 8. HTTP — aprovação interna e reagendamento (Subfase 4.1)

Todas as rotas exigem JWT tenant, header `X-Tenant-Id`, permissão **`social.copilot.generate`** e query **`workspaceId`**. Corpo vazio em submit, approve e reject.

### `POST /api/v1/social/posts/{id}:submit`

Transição `Draft` → `PendingApproval`.

### `POST /api/v1/social/posts/{id}:approve`

Transição `PendingApproval` → `Approved` (não abre job outbox; use `:schedule` em seguida).

### `POST /api/v1/social/posts/{id}:schedule`

Transição `Approved` → `Scheduled` e criação de `PublicationJob` em `Pending` com chave `{postId:D}:{scheduledAtUtc:O}` no mesmo `CommitAsync` do post.

### `POST /api/v1/social/posts/{id}:reject`

Transição `PendingApproval` → `Rejected`.

### `POST /api/v1/social/posts/{id}:reschedule`

Corpo:

```json
{
  "scheduledAtUtc": "2026-07-01T14:00:00Z"
}
```

Resposta de sucesso (`Result<OrganicPostDto>`):

```json
{
  "isSuccess": true,
  "value": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "workspaceId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "connectionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "platform": "Instagram",
    "format": "FeedImage",
    "pillar": "Authority",
    "caption": "Legenda",
    "scheduledAtUtc": "2026-07-01T14:00:00Z",
    "status": "Draft"
  }
}
```

| Cenário | HTTP | Código |
| :--- | :---: | :--- |
| Post de outro workspace ou inexistente | **404** | `Social.PostNotFound` |
| Transição ilegal (ex.: approve em `Draft`) | **409** | `Social.InvalidTransition` |
| Reagendar em `Publishing` / `Published` / `Cancelled` | **409** | `Social.SlotNotReschedulable` |
| `Scheduled` com horário no passado | **422** | `Social.ScheduleInThePast` |
| `scheduledAtUtc` sem kind UTC | **422** | `Social.ScheduleInstantNotUtc` |

---

## 9. Outbox de publicação (Subfase 4.2)

### Ciclo de vida do `PublicationJob`

| Status | Significado |
| :--- | :--- |
| `Pending` | Aguardando horário ou claim do worker. |
| `InProgress` | Reivindicado por um worker (claim otimista). |
| `RetryScheduled` | Retentativa após rate limit ou falha recuperável; aguarda `NextAttemptAtUtc`. |
| `Succeeded` | Publicação concluída. |
| `DeadLetter` | Falha definitiva após teto de tentativas (`OrganicPublishOptions.MaxAttempts`). |
| `Cancelled` | Slot substituído por reagendamento. |

Chave de idempotência (índice único): `{postId:D}:{scheduledAtUtc:O}` em UTC, cultura invariante.

Reagendamento de post `Scheduled`: cancela job ativo (`Pending` / `RetryScheduled`), persiste revisão do post e abre novo job com chave nova no mesmo `CommitAsync`. Job `InProgress` bloqueia reagendamento → `Social.PublicationJobInProgress`.

Claim (`IPublicationJobRepository.TryClaimAsync`): aceita apenas `Pending` ou `RetryScheduled`; marca `InProgress` e persiste com `RowVersion`. Segundo claim concorrente recebe `null`.

### Exemplo — job após `:schedule`

```json
{
  "id": "8b1e2f3a-4c5d-6e7f-8091-a2b3c4d5e6f7",
  "postId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "workspaceId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "scheduledAtUtc": "2026-07-01T14:00:00.0000000Z",
  "idempotencyKey": "3fa85f64-5717-4562-b3fc-2c963f66afa6:2026-07-01T14:00:00.0000000Z",
  "status": "Pending",
  "attemptCount": 0,
  "nextAttemptAtUtc": null,
  "createdAtUtc": "2026-06-15T12:00:00.0000000Z"
}
```

### Casos de borda (outbox)

| Cenário | Comportamento |
| :--- | :--- |
| Segundo insert mesma chave | `Social.PublicationJobAlreadyExists`; uma linha no banco. |
| Dois `TryClaimAsync` concorrentes | Um job `InProgress`; o outro `null`. |
| Claim em `Succeeded` / `Cancelled` / `DeadLetter` | `null`, sem alteração. |
| Reagendar com job `InProgress` | **409** `Social.PublicationJobInProgress`; sem `CommitAsync`. |

### Worker de disparo (Subfase 4.4)

Sem rota HTTP. O host registra `SocialPublishingDispatchWorker` com `SocialPublishing:Dispatch:Enabled` **false** por padrão e `IntervalSeconds` (mínimo 5).

Fluxo por tenant (`Active` / `Trial`):

1. Kill switch `killswitch.social-publishing.dispatch` armado → ciclo encerra sem despacho.
2. `ListDueAsync(utcNow)` → `Pending` com `scheduledAtUtc <= now` ou `RetryScheduled` com `nextAttemptAtUtc <= now`.
3. `TryClaimAsync` → `InProgress`; post deve estar `Scheduled`.
4. `MarkPublishing` → adaptador `PublishAsync` (token decifrado, URLs via `IOrganicMediaPublicUrlResolver` + `SocialPublishing:Media:PublicBaseUrl`).
5. Sucesso → `MarkPublished(externalId)`, job `Succeeded`, `CommitAsync`.
6. `Social.PublishRateLimited` / `Social.PublishFailed` → se restam tentativas, `RetryScheduled` e post `Scheduled`; senão `DeadLetter` e post `Failed`.
7. `NextAttemptAtUtc` = `utcNow + max(OrganicPublishRateLimitPolicy, PublishRateLimitedError.RetryAfter)`.

| Código | Efeito no worker |
| :--- | :--- |
| `Social.PublishRateLimited` | Retentativa com backoff + `Retry-After` quando presente |
| `Social.PublishFailed` | Mesma régua de retentativa, sem `Retry-After` |
| Teto de tentativas | `DeadLetter` + post `Failed`; adaptador não é chamado |
| Cancelamento do host durante publicação | Job pode permanecer `InProgress` (sem reclaim nesta subfase) |

---

## 10. Adaptadores de publicação (Subfase 4.3)

Publicação orgânica na borda HTTP: `IOrganicPlatformAdapter.PublishAsync` recebe token **já decifrado** e URLs HTTPS de mídia (`OrganicPublishRequest`). O adaptador não abre cofre, não lê `IOrganicMediaStore` e não loga o token. Meta envia `access_token` no corpo `application/x-www-form-urlencoded`; TikTok usa `Authorization: Bearer`.

### Formato → adaptador → operação de rede

| Formato | Adaptador | Sequência (Graph `MetaOrganicOAuthOptions.GraphApiVersion` ou TikTok Content Posting) |
| :--- | :--- | :--- |
| `FeedImage` | `MetaFacebookPageOrganicAdapter` | `POST /{page-id}/photos` (`url`, `caption`, `published=true`) |
| `FeedCarousel` | `MetaInstagramOrganicAdapter` | `POST /{ig-user-id}/media` por item (`is_carousel_item=true`) → container `media_type=CAROUSEL` + `children` → `POST .../media_publish` |
| `Reel` | `MetaInstagramOrganicAdapter` | `POST /media` (`media_type=REELS`, `video_url`) → `GET /{container-id}?fields=status_code` (exige `FINISHED`) → `media_publish` |
| `TikTokVideo` | `TikTokContentPostingAdapter` | `POST .../video/init/` (`PULL_FROM_URL`) → `POST .../status/fetch/` (`PUBLISH_COMPLETE`) |

`OrganicPublishOptions` (`SocialPublishing:Publish`): `MaxAttempts` (3), `BaseDelayMs` (500), `MaxDelayMs` (10_000), `TikTokPrivacyLevel` (`PUBLIC_TO_EVERYONE`).

Backoff puro no domínio: `OrganicPublishRateLimitPolicy.CalculateDelayMs(attempt, jitterMs)` = `min(2^attempt × BaseDelayMs + jitter, MaxDelayMs)`. O worker (4.4) aplica o atraso e combina com `PublishRateLimitedError.RetryAfter`.

### Payload de entrada (escopo do handler 4.4)

```json
{
  "accessToken": "<decifrado no handler, nunca persistido>",
  "externalAccountId": "page-or-ig-or-open-id",
  "format": "FeedCarousel",
  "caption": "Legenda final",
  "media": [
    { "sourceUrl": "https://cdn.agencia/obj.jpg", "sortOrder": 0 }
  ]
}
```

### Resposta de sucesso (`Result<OrganicPublishReceipt>`)

```json
{
  "isSuccess": true,
  "value": {
    "externalId": "17841400000000000"
  }
}
```

### Erros de borda (publicação)

| Cenário | Código | Observação |
| :--- | :--- | :--- |
| HTTP 429 com `Retry-After` | `Social.PublishRateLimited` | `PublishRateLimitedError.RetryAfter` preenchido quando o header existir |
| HTTP 429 sem header | `Social.PublishRateLimited` | `RetryAfter` nulo; worker usa política pura |
| Erro funcional / 4xx–5xx Meta ou TikTok | `Social.PublishFailed` | Sem exceção propagada ao handler |
| Reel com container não `FINISHED` na primeira leitura | `Social.PublishFailed` | Retentativa fica a cargo do worker |
| Formato fora do adaptador (ex.: Reel na Page) | `Social.PublishFormatUnsupported` | Nenhuma chamada HTTP |

---

## 11. Frontend Blazor — conexões orgânicas (Subfase 5.1)

O **`WebApp`** consome **somente** a Web API via **`ISocialPublishingClient`** / **`SocialPublishingClient`** (`AddHttpClient` em `Program.cs`). É **proibido** injetar `DbContext`, repositórios, MediatR ou referenciar `SocialPublishing.Infrastructure`.

### Rotas e componentes

| Rota Blazor | Layout | Componente |
| :--- | :--- | :--- |
| `/social/connections` | `TenantMainLayout` (white-label via `TenantStateProvider`) | `SocialConnectionsPage` + `SocialConnectionsPanel` |

Peças atômicas: **`OrganicConnectionStatusBadge`** em `Components/Shared/Components/`. Navegação lateral: item **Redes orgânicas** (`TenantSidebar`).

### Cliente HTTP

Toda requisição autenticada envia **`Authorization: Bearer`** (sessão do tenant) e **`X-Tenant-Id`** (estado do inquilino), alinhado aos demais clientes do `WebApp`.

| Método | API |
| :--- | :--- |
| `ListConnectionsAsync` | `GET /api/v1/social/connections?workspaceId=` |
| `StartAuthorizationAsync` | `POST /api/v1/social/connections/authorize` |
| `CompleteAuthorizationAsync` | `POST /api/v1/social/connections/callback` |
| `RevokeConnectionAsync` | `DELETE /api/v1/social/connections/{id}?workspaceId=` |

Falhas de rede retornam `Result.Failure` com código `Social.NetworkError` (sem exceção de fluxo na UI). O `RedirectUri` do OAuth é a própria URL canônica de `/social/connections` (mesmo padrão de `/integrations` para mídia paga).

Testes: **`SocialPublishingClientTests`**, **`SocialConnectionsPanelTests`** (bUnit) em `tests/UnitTests/Frontend/`.

---

## 11.1 Frontend Blazor — calendário orgânico (Subfase 5.2)

Grade mensal e semanal com reagendamento por arrastar e soltar. Toda mutação passa por **`POST /api/v1/social/posts/{id}:reschedule`**; o JavaScript não contém regra de negócio.

### Rotas e componentes

| Rota Blazor | Layout | Componente |
| :--- | :--- | :--- |
| `/social/calendar` | `TenantMainLayout` | `SocialCalendarPage` + `SocialCalendarGrid` |

Peças atômicas: **`OrganicPostStatusBadge`**, **`ContentPillarChip`**. Navegação lateral: **Calendário orgânico** (`TenantSidebar`).

### Modos da grade

| Modo | Comportamento |
| :--- | :--- |
| **Month** | Mês civil da data âncora; semana começa na **segunda-feira**; células de preenchimento do mês anterior e seguinte. |
| **Week** | Sete colunas da semana que contém a data âncora (segunda a domingo). |

Cada post é posicionado no dia civil obtido ao converter **`ScheduledAtUtc`** com o fuso IANA informado (padrão da página: **`America/Sao_Paulo`**). O **`WorkspaceDto`** ainda **não** expõe fuso; a UI usa constante até persistência no módulo Tenants.

### Contrato do drop (reagendamento)

1. Só posts em **`Draft`**, **`PendingApproval`**, **`Approved`** ou **`Scheduled`** exibem alça de arraste.
2. **`Published`**, **`Publishing`** e **`Cancelled`** não são arrastáveis (`Social.SlotNotReschedulable` no backend se forçado).
3. Soltar em **outro dia civil** (mesmo fuso): calcula novo **`scheduledAtUtc`** preservando **hora e minuto locais** do slot original; chama **`RescheduleAsync`**.
4. Soltar no **mesmo dia**: nenhuma chamada HTTP.
5. **`Result` de falha**: o card volta à célula de origem e a descrição do erro aparece na grade (`data-testid="social-calendar-error"`).
6. Troca de workspace na barra lateral: a página limpa a lista e chama **`ListPostsAsync`** de novo (sem vazar posts de outro cliente).

### Cliente HTTP (calendário)

| Método | API |
| :--- | :--- |
| `ListPostsAsync` | `GET /api/v1/social/posts?workspaceId=&from=&to=` (UTC inclusivo) |
| `RescheduleAsync` | `POST /api/v1/social/posts/{id}:reschedule?workspaceId=` corpo `{ "scheduledAtUtc": "..." }` |

Permissões: leitura **`social.read`**; reagendamento **`social.copilot.generate`** (mesma rota da Subfase 4.1).

Testes: **`SocialCalendarGridTests`**, extensões em **`SocialPublishingClientTests`**.

---

## 11. Portal de aprovação compartilhável (Subfase 5.3)

Link canônico: `https://{dominio-da-agencia}/approve/{token}`. Sem CNAME: `/t/{slug}/approve/{token}`. Token opaco (32 bytes, Base64Url) persistido **somente** como SHA-256 hex (64 caracteres). Um link ativo por workspace; nova emissão revoga o anterior.

### Erros

| Código | Quando |
| :--- | :--- |
| `Social.ApprovalTokenInvalid` | Token ausente, malformado, expirado ou revogado (mesma resposta HTTP 404 pública). |
| `Social.ApprovalLinkExpirationInvalid` | Expiração ausente ou no passado na emissão autenticada. |
| `Social.ApprovalCommentRequired` | `RequestChanges` sem comentário. |
| `Social.ApprovalPostSelectionRequired` | Decisão sem posts selecionados. |

### `POST /api/v1/social/approval-links` (autenticado)

Permissão: `social.copilot.generate`.

**Entrada**

```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "expiresAtUtc": "2026-06-30T23:59:59Z"
}
```

**Sucesso (`200`, token em claro só aqui)**

```json
{
  "isSuccess": true,
  "value": {
    "plainToken": "xY9k…",
    "expiresAtUtc": "2026-06-30T23:59:59Z",
    "relativePath": "/approve/xY9k…"
  }
}
```

### `GET /api/v1/public/social/approvals/{token}` (anônimo)

Tenant resolvido antes do banco (`X-Tenant-Id` slug ou `X-Forwarded-Host`). Handler Social retorna posts; Web API compõe branding.

**Sucesso (`200`)**

```json
{
  "isSuccess": true,
  "value": {
    "posts": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "caption": "Legenda…",
        "platform": "Instagram",
        "format": "FeedImage",
        "pillar": "Authority",
        "scheduledAtUtc": "2026-07-01T13:00:00Z"
      }
    ],
    "primaryColor": "#2563EB",
    "logoUrl": "https://cdn/logo.png",
    "agencyName": "Agência Alfa"
  }
}
```

**Indisponível (`404`, mesmo corpo para expirado/inexistente/revogado)**

```json
{
  "isSuccess": false,
  "error": {
    "code": "Social.ApprovalTokenInvalid",
    "description": "O link de aprovação não está disponível."
  }
}
```

### `POST /api/v1/public/social/approvals/{token}/decisions` (anônimo)

```json
{
  "postIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"],
  "decision": "Approve",
  "comment": null
}
```

`decision`: `Approve` | `RequestChanges` (comentário obrigatório).

### Blazor

| Rota | Layout | Componente |
| :--- | :--- | :--- |
| `/approve/{token}` | `EmptyLayout` | `PublicApprovalPage` |
| `/t/{slug}/approve/{token}` | `EmptyLayout` | `PublicApprovalPage` |

Calendário autenticado: botão **Link de aprovação** em `/social/calendar` (`CreateApprovalLinkAsync`).

Testes: `CreateApprovalLinkCommandTests`, `PublicApprovalQueryAndCommandTests`, `PublicApprovalEndpointTests`, `PublicApprovalPageTests`.

ADR: [0047](../adr/0047-white-label-organic-approval-portal.md).

---

## 11.4 Frontend Blazor — copilot (Subfase 5.4)

Formulário de **Brand Voice** e diálogo **Gerar cronograma** no calendário. Toda leitura e mutação passa por **`ISocialPublishingClient`**; é **proibido** injetar `DbContext`, repositórios ou MediatR no `WebApp`.

### Fluxo ponta a ponta (gestor)

1. **Voz da marca** — em `/social/brand-voice`, salvar tom, persona, links, termos, emoji e pesos dos pilares (`PUT /api/v1/social/brand-voice/{workspaceId}`). A UI **bloqueia** salvar se a soma dos quatro pesos ≠ 100; validações adicionais continuam no backend (`Social.PillarWeightsInvalid`, etc.).
2. **Gerar cronograma** — no calendário (`/social/calendar`), botão **Gerar cronograma** abre `GeneratePlanDialog` e chama `POST /api/v1/social/plans:generate` com intervalo (`DateOnly`), `postsPerWeek`, `seed` e fuso IANA (**`America/Sao_Paulo`**, constante da página até o `WorkspaceDto` expor fuso).
3. **Revisar na grade** — posts em **`Draft`** aparecem na grade após fechar o diálogo com sucesso (total ou parcial); a página recarrega `ListPostsAsync`.
4. **Aprovação interna / agendamento** — fluxos HTTP das Subfases 4.x (`:submit`, `:approve`, `:schedule`, …) permanecem na API; a UI de lifecycle por post pode evoluir depois desta subfase.
5. **Link público** — botão **Link de aprovação** na toolbar do calendário (`CreateApprovalLinkAsync`); cliente final aprova em `/approve/{token}` ou `/t/{slug}/approve/{token}` (Subfase 5.3).

### Rotas e componentes

| Rota Blazor | Layout | Componente |
| :--- | :--- | :--- |
| `/social/brand-voice` | `TenantMainLayout` | `SocialBrandVoicePage` + `BrandVoiceForm` |
| `/social/calendar` (ação) | `TenantMainLayout` | `GeneratePlanDialog` (modal) |

Navegação lateral: **Voz da marca** (`TenantSidebar`), junto a Redes orgânicas e Calendário orgânico.

### Brand Voice (UI)

| Comportamento | Detalhe |
| :--- | :--- |
| Carregamento | `GetBrandVoiceAsync`; `Social.BrandVoiceNotFound` → formulário vazio **sem** alerta de erro |
| Soma dos pesos | Exibida em tempo real; botão salvar desabilitado se ≠ 100 |
| Falha no PUT | Descrição do erro em `Result`; campos digitados **não** são limpos |
| Troca de workspace | `@key` no formulário recarrega o perfil do cliente ativo |

### Gerar cronograma (UI)

| Cenário | UI |
| :--- | :--- |
| HTTP **200** com `failedSlots` | Lista cada slot (data local, pilar, rede, `errorDescription`); mostra quantidade de `createdPostIds` e link **Ver no calendário orgânico** (`href="social/calendar"`) |
| `Result` de falha (ex. Brand Voice ausente) | Mensagem de erro; **sem** lista de slots nem link de posts criados |
| Fechar após sucesso | `OnGenerationSucceeded` → `ListPostsAsync` na página do calendário |

Corpo HTTP gerado pelo cliente (espelha §7.4):

```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "rangeStart": "2026-11-02",
  "rangeEnd": "2026-11-06",
  "timeZoneId": "America/Sao_Paulo",
  "postsPerWeek": 5,
  "seed": 0
}
```

### Cliente HTTP (copilot)

| Método | API |
| :--- | :--- |
| `GetBrandVoiceAsync` | `GET /api/v1/social/brand-voice/{workspaceId}` |
| `UpsertBrandVoiceAsync` | `PUT /api/v1/social/brand-voice/{workspaceId}` |
| `GeneratePlanAsync` | `POST /api/v1/social/plans:generate` |

Permissões: leitura de Brand Voice **`social.read`**; upsert e geração **`social.copilot.generate`**. Falhas de rede → `Social.NetworkError` no cliente (sem exceção de fluxo na UI).

Testes: **`BrandVoiceFormTests`**, **`GeneratePlanDialogTests`**, extensões em **`SocialPublishingClientTests`**.

---

## 12. Detecção de tração orgânica (Subfase 6.1)

Objetivo: identificar posts publicados com engajamento anômalo em relação à mediana do **mesmo workspace** na **mesma rede**, sem persistir sugestão, sem MediatR para outros módulos e sem rota HTTP nesta subfase.

### Fórmula e limiares

Constantes em `OrganicTractionThresholds`:

| Constante | Valor | Uso |
| :--- | :--- | :--- |
| `MinimumSampleSize` | 3 | Mínimo de posts com impressões &gt; 0 na janela, por rede |
| `BaselineWindowDays` | 28 | Janela retroativa a partir de `asOfUtc` |
| `MinimumImpressions` | 1000 | Impressões mínimas para candidato a hit |
| `EngagementMultiple` | 2,0 | Post é hit se taxa ≥ múltiplo × mediana |

**Taxa de engajamento** (por post):

\[
\text{engajamento} = \frac{\text{curtidas} + \text{comentários} + \text{compartilhamentos} + \text{salvamentos}}{\text{impressões}}
\]

**Linha de base:** mediana das taxas dos posts elegíveis na janela (mesma rede, mesmo workspace). Posts com impressões zero ficam fora da janela. A mediana inclui o candidato e posts que já possuem sugestão `Open` (para não inflar artificialmente o limiar).

**Hit:** impressões ≥ `MinimumImpressions` **e** engajamento ≥ `EngagementMultiple` × mediana (igualdade nos dois limiares conta).

**Dados insuficientes:** se **nenhuma** rede atinge `MinimumSampleSize` na janela → `OrganicTraction.InsufficientData` e nenhum hit.

**Sugestão já aberta:** post que passaria no limiar, mas `HasOpenSuggestion = true`, não entra em `Hits`; o relatório lista `SkippedDueToOpenSuggestion` com `Social.BoostAlreadyRequested`.

### Domínio puro

`OrganicTractionDetector.Detect(workspaceId, series, asOfUtc)` em `SocialPublishing.Domain/Traction/`:

- Entrada: `IReadOnlyList<OrganicPostInsightSample>` (métricas já conhecidas pelo chamador).
- Saída: `Result<OrganicTractionReport>` com `Hits` e `SkippedDueToOpenSuggestion`.
- Função pura: mesmo input → mesmo output; sem `DateTime.UtcNow` interno.

Testes: `OrganicTractionDetectorTests`.

### Porta de insights (`IOrganicInsightsReader`)

Coleta de métricas na rede fica atrás de `IOrganicInsightsReader.ReadAsync(OrganicInsightsReadRequest)` → `Result<OrganicPostInsightSnapshot>`. O request traz token **já decifrado**, `ExternalAccountId`, `PublishedExternalId` e `OrganicPlatform`. Falha de leitura → `Social.InsightsReadFailed` (401 → `Social.TokenRevoked`). O adaptador **não** abre cofre, **não** persiste sugestão e **não** referencia repositório.

Implementação: `HttpOrganicInsightsReader` (`SocialPublishing.Infrastructure/Insights/`).

| Rede | Chamada | Métricas mapeadas |
| :--- | :--- | :--- |
| Instagram | `GET /{media-id}/insights?metric=impressions,likes,comments,shares,saved` | Todas as cinco |
| Facebook Page | `GET /{post-id}/insights?metric=post_impressions,post_reactions_like_total,post_comments,post_shares` | Salvamentos = 0 |
| TikTok | `POST https://open.tiktokapis.com/v2/video/query/` (`video_ids`) | `view_count` → impressões; salvamentos = 0 |

Autenticação: header `Authorization: Bearer`; token **não** aparece na URI. Versão Graph: `SocialPublishing:OAuth:Meta:GraphApiVersion` (padrão `v26.0`).

Registro DI: `AddHttpClient<IOrganicInsightsReader, HttpOrganicInsightsReader>().RemoveAllLoggers()` em `AddSocialPublishingModule`.

Testes: `HttpOrganicInsightsReaderTests`.

### Scan, sugestão e notificação (Subfase 6.2)

Comando interno `ScanOrganicTractionCommand` (sem rota HTTP). O `SocialPublishingDispatchWorker` executa, **por tenant**, `DispatchDuePublicationsCommand` e em seguida `ScanOrganicTractionCommand`, respeitando o mesmo kill switch `killswitch.social-publishing.dispatch`.

Fluxo do scan:

1. Lista posts `Published` desde `asOfUtc − 28 dias` via `ListPublishedForTractionAsync` (usa `ScheduledAtUtc` como instante de publicação e `PublishedExternalId` obrigatório).
2. Por workspace, lê métricas na rede com `IOrganicInsightsReader` (token decifrado como no disparo). Falha de insights → post ignorado; o comando continua.
3. Monta `OrganicPostInsightSample` com `HasOpenSuggestion` a partir de sugestões `Open` já persistidas.
4. `OrganicTractionDetector.Detect` — mesmas regras da subfase 6.1.
5. Para cada hit: `PaidBoostSuggestion.Open` → `CommitAsync` → **somente então** `IPublisher.Publish(OrganicTractionDetectedNotification)`.

**Fingerprint de mídia:** SHA-256 hexadecimal (UTF-8) da `ObjectKey` do primeiro item de mídia (`OrganicMediaFingerprint.FromObjectKey`). Sem chave → `MediaFingerprint` nulo na notificação.

**Contrato** (`SocialPublishing.Contracts/Notifications/OrganicTractionDetectedNotification`):

```json
{
  "tenantId": "guid",
  "workspaceId": "guid",
  "organicPostId": "guid",
  "network": "Instagram",
  "mediaFingerprint": "a1b2…",
  "permalink": null,
  "impressions": 5000,
  "engagementRate": 0.05,
  "baselineMedianEngagement": 0.02,
  "engagementMultiple": 2.5,
  "detectedAtUtc": "2026-11-03T15:00:00Z"
}
```

**Persistência social:** tabela `social.PaidBoostSuggestions`, índice único filtrado `(WorkspaceId, OrganicPostId)` onde `Status = Open`.

**Creative Hub (Analytics):** handler `LinkOrganicAssetWhenTractionDetected` faz upsert em `analytics.CreativeHubAssets` pela `(WorkspaceId, MediaFingerprint)`. Fingerprint ausente → log com `Analytics.OrganicFingerprintMissing`; nenhuma linha gravada; o post orgânico permanece publicado.

Testes: `ScanOrganicTractionCommandTests`, `LinkOrganicAssetWhenTractionDetectedTests`, extensões em `SocialPublishingDispatchWorkerTests`. ADR: [0048](../adr/0048-organic-traction-boost-and-creative-hub.md).

---

## 13. Aceite da sugestão e rascunho pausado (Subfase 6.3)

Comandos `AcceptPaidBoostSuggestionCommand` e `DismissPaidBoostSuggestionCommand`. Aceite valida sugestão `Open`, carrega legenda e `ObjectKey` da primeira mídia do post orgânico e envia `CreatePaidBoostDraftCommand` (contrato em `SocialPublishing.Contracts`). Integrations persiste `PaidBoostDraft` **pausado** no banco operacional do tenant; nenhuma escrita na rede de anúncios nesta subfase.

### Rotas HTTP

| Método | Rota | Permissão |
| :--- | :--- | :--- |
| `GET` | `/api/v1/social/boost-suggestions?workspaceId=` | `social.read` |
| `POST` | `/api/v1/social/boost-suggestions/{id}:accept?workspaceId=` | `social.copilot.generate` |
| `POST` | `/api/v1/social/boost-suggestions/{id}:dismiss?workspaceId=` | `social.copilot.generate` |

### Exemplo — listagem (`Result` 200)

```json
{
  "isSuccess": true,
  "value": [
    {
      "id": "guid",
      "organicPostId": "guid",
      "platform": "Instagram",
      "status": "Open",
      "impressions": 5000,
      "engagementRate": 0.05,
      "engagementMultiple": 2.5,
      "detectedAtUtc": "2026-11-03T15:00:00Z",
      "paidDraftId": null
    }
  ],
  "error": null
}
```

### Exemplo — aceite com sucesso (`Result` 200)

Mesmo envelope com `status: "Accepted"` e `paidDraftId` preenchido com o identificador do rascunho local.

### Erros de borda

| Cenário | HTTP | Código |
| :--- | :--- | :--- |
| Sugestão ausente ou de outro workspace | **404** | `Social.BoostSuggestionNotFound` |
| Aceite/dispensa fora de `Open` | **409** | `Social.InvalidTransition` |
| Post orgânico ausente | **404** | `Social.PostNotFound` |
| Sem conta Meta conectada | **404** | `Meta.AccountNotLinked` |
| Sem conta TikTok Ads conectada | **404** | `SyncCampaignHierarchy.AccountNotFound` |
| Falha do Integrations no aceite | mesmo status do erro | código original; sugestão permanece `Open` |

Testes: `AcceptPaidBoostSuggestionCommandTests`, `CreatePaidBoostDraftCommandHandlerTests`, `IntegrationsApplication_MayReferenceSocialPublishingContractsOnly`.

---

## 14. Fora de escopo (após Subfase 6.3)

| Área | Entrega futura |
| :--- | :--- |
| OAuth orgânico (continuação) | Refresh preventivo e revogação remota na rede (Subfase 2.3) |
| Reclaim de jobs `InProgress` órfãos | Timeout operacional ou job dedicado |
| Lifecycle de posts na UI | Ações `:submit` / `:approve` / `:schedule` por card na grade |
| Sinergia paga (continuação) | Banner Blazor e cliente HTTP na grade (Subfase 6.4) |

---

## 15. Composição na Web API

Registro em `Program.cs`:

```csharp
builder.Services.AddSocialPublishingModule(builder.Configuration);
builder.Services.AddSocialPublishingTenantPersistenceComposition();
```

`AddSocialPublishingModule` registra MediatR, repositórios, fábrica de `SocialPublishingDbContext` e `ISocialPublishingSchemaMigrator`. `AddSocialPublishingTenantPersistenceComposition` decora `ITenantProvisioningService` (migrações **social** e **analytics**) e registra o hosted service de migração para tenants existentes.

Worker de disparo (mesmo `Program.cs`):

```csharp
builder.Services.AddOptions<SocialPublishingDispatchOptions>()
    .BindConfiguration(SocialPublishingDispatchOptions.SectionName);
builder.Services.AddHostedService<SocialPublishingDispatchWorker>();
```
