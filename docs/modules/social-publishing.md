# Especificação Funcional & Técnica: Social Publishing (AI Social Copilot & Scheduling)

## 1. Visão geral

O bounded context **SocialPublishing** cobre gestão e publicação orgânica na plataforma AdMetricsPro: conexão com Meta (Instagram Professional/Business e Facebook Pages) e TikTok Content Posting API, geração de cronograma e criativos com IA, calendário com aprovação white-label e sugestão de impulsionamento quando um post orgânico ganha tração.

Até a **Subfase 3.1** o módulo inclui o **modelo de domínio puro**, **persistência EF no schema `social`**, `GET /api/v1/social/status`, permissões RBAC `SocialRead` (`social.read`), **`SocialConnectionsManage`** (`social.connections.manage`), **`SocialMediaManage`** (`social.media.manage`) e **`SocialCopilotGenerate`** (`social.copilot.generate`), **state OAuth HMAC-SHA256**, adaptadores com **`ExchangeCodeAsync`**, **`OrganicTokenProtector`**, rotas OAuth **`authorize`** / **`callback`**, porta **`IOrganicMediaStore`** (local ou S3-compatível), **`POST /api/v1/social/media`** e **ingestão de Brand Voice** (`PUT` / `GET` `/api/v1/social/brand-voice/{workspaceId}`). Refresh/revoke, listagem HTTP de conexões, planner editorial, legendas IA, worker, Blazor e sinergia paga permanecem fora desta entrega (ver ADR 0044).

Stack e limites (detalhados no ADR 0043):

- .NET 10: `SocialPublishing.Domain`, `SocialPublishing.Contracts`, `SocialPublishing.Application`, `SocialPublishing.Infrastructure`, controllers em `WebApi` e consumo futuro via `HttpClient` no `WebApp`.
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
| `Social.HashtagPolicyViolated` | Hashtag duplicada, com espaço ou acima do teto configurado. |
| `Social.PillarWeightsInvalid` | Pesos dos pilares somam valor diferente de 100 ou algum peso é negativo. |
| `Social.BrandVoiceNotFound` | Workspace sem DNA de marca persistido. |
| `Social.SlotNotReschedulable` | Post em `Publishing`, `Published` ou `Cancelled`. |
| `Social.InvalidTransition` | Transição de status não permitida pela máquina de estados. |
| `Social.ApprovalTokenInvalid` | Token ausente, expirado, revogado ou de outro workspace. |
| `Social.PublishRateLimited` | Rede respondeu 429 ou quota esgotada. |
| `Social.PublishFailed` | Falha definitiva após o teto de tentativas. |
| `Social.BoostAlreadyRequested` | Já existe sugestão aberta para o mesmo post. |

Todas as operações futuras devem retornar `Result` / `Result<T>` com esses códigos estáveis, sem `throw` para regra de negócio.

---

## 4. Modelo de domínio (Subfase 1.2)

Implementação em `SocialPublishing.Domain`, sem Entity Framework. Fábricas estáticas retornam `Result` / `Result<T>`; regras de negócio não lançam exceção.

### Agregados

| Tipo | Responsabilidade |
| :--- | :--- |
| `OrganicConnection` | Conexão OAuth orgânica por workspace; tokens apenas como `EncryptedAccessToken` / `EncryptedRefreshToken`; `Revoke` idempotente emite `OrganicConnectionRevoked`. |
| `OrganicPost` | Post agendado com máquina de estados, legenda, mídia (dimensões) e `ScheduledAtUtc`. |
| `BrandVoiceProfile` | DNA de marca: tom, persona, links absolutos, termos proibidos, `EmojiPolicy`, teto de hashtags e pesos dos pilares. |

### Value objects e constantes

| Tipo | Invariantes |
| :--- | :--- |
| `AspectRatio` / `AspectRatioRules` | Proporções nomeadas com tolerância relativa de **1%**: `1:1`, `4:5` (feed/carrossel), `9:16` (Reel/TikTok). Constantes: `AspectRatioRules.Label11`, `Label45`, `Label916`. |
| `HashtagGroup` | Sem duplicata (case insensitive, `#` opcional), sem espaço interno; quantidade ≤ `min(tetoDoPerfil, tetoDaRede)` (Instagram/Page: 30, TikTok: 8). |
| `PillarWeights` | Quatro inteiros ≥ 0 somando exatamente 100. |
| `CaptionLimits` | Instagram, Facebook Page e TikTok: **2200** caracteres (constantes nomeadas, revisáveis). |

### Transições de `OrganicPost`

| De | Para (permitido) |
| :--- | :--- |
| `Draft` | `PendingApproval`, `Cancelled` |
| `PendingApproval` | `Approved`, `Rejected`, `ChangesRequested`, `Cancelled` |
| `Approved` | `Scheduled`, `Cancelled` |
| `Scheduled` | `Publishing`, `Cancelled` |
| `Publishing` | `Published`, `Failed` |
| `Failed` | `Cancelled` |

Reagendamento (`Reschedule`): permitido em `Draft`, `PendingApproval`, `Approved`, `Scheduled`. Entrada: data/hora **local** + fuso IANA do workspace; persistência só em UTC. Bloqueado em `Publishing`, `Published`, `Cancelled` → `Social.SlotNotReschedulable`.

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

**Fora da Subfase 2.2:** nonce/state de uso único persistido em banco (state continua stateless; o code é single-use no provedor), refresh preventivo, revoke e listagem HTTP (Subfase 2.3).

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

## 8. Fora de escopo (após Subfase 3.1)

| Área | Entrega futura |
| :--- | :--- |
| OAuth orgânico (continuação) | Refresh, revoke, listagem HTTP (Subfase 2.3) |
| IA / copiloto (continuação) | Planner editorial, legendas e plano materializado (Subfases 3.2–3.4) |
| Worker | Outbox, `SocialPublishingDispatchWorker`, kill switch (Fase 4) |
| Blazor | Calendário, portal `/approve/{token}` (Fase 5) |
| Sinergia paga | Boost e Creative Hub (Fase 6) |

---

## 9. Composição na Web API

Registro em `Program.cs`:

```csharp
builder.Services.AddSocialPublishingModule(builder.Configuration);
builder.Services.AddSocialPublishingTenantPersistenceComposition();
```

`AddSocialPublishingModule` registra MediatR, repositórios, fábrica de `SocialPublishingDbContext` e `ISocialPublishingSchemaMigrator`. `AddSocialPublishingTenantPersistenceComposition` decora `ITenantProvisioningService` e registra o hosted service de migração social para tenants existentes.
