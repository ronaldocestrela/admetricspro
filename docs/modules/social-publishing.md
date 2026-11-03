# Especificação Funcional & Técnica: Social Publishing (AI Social Copilot & Scheduling)

## 1. Visão geral

O bounded context **SocialPublishing** cobre gestão e publicação orgânica na plataforma AdMetricsPro: conexão com Meta (Instagram Professional/Business e Facebook Pages) e TikTok Content Posting API, geração de cronograma e criativos com IA, calendário com aprovação white-label e sugestão de impulsionamento quando um post orgânico ganha tração.

Até a **Subfase 2.1** o módulo inclui o **modelo de domínio puro**, **persistência EF no schema `social`**, `GET /api/v1/social/status`, permissão RBAC `SocialRead` (`social.read`), **state OAuth HMAC-SHA256** (`IOrganicOAuthStateService`), adaptadores orgânicos (`IOrganicPlatformAdapter`) e a consulta **`StartOrganicAuthorizationQuery`** (sem rota HTTP ainda). Callback, cofre AES-256 em `OrganicConnection`, refresh/revoke, mídia, IA, worker, Blazor e sinergia paga permanecem fora desta entrega (ver ADR 0044).

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
| `Social.PlatformUnsupported` | Rede fora de `Instagram`, `FacebookPage`, `TikTok`. |
| `Social.MediaAspectInvalid` | Mídia fora da proporção exigida pelo formato. |
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
| `AspectRatio` | Proporção 9:16 para Reels/TikTok com tolerância relativa de 1% (`\|w/h − 9/16\| ≤ 0,01 × 9/16`). |
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
| `OrganicMediaAssets` | `Id`, `OrganicPostId`, `Width`, `Height`, `SortOrder` | FK → `OrganicPosts` (cascade) |
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

## 6.1 OAuth orgânico — início do fluxo (Subfase 2.1)

Consulta MediatR **`StartOrganicAuthorizationQuery`** (Application). **Sem endpoint HTTP** nesta subfase; a rota `POST /api/v1/social/connections/authorize` entra na Subfase 2.2 com permissão `social.connections.manage`.

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

### Fora da Subfase 2.1

Callback (`CompleteOrganicAuthorizationCommand`), persistência de tokens com AES-256, refresh, revoke, listagem HTTP e registro de state de uso único em banco (Subfase 2.2).

---

## 7. Fora de escopo (após Subfase 2.1)

| Área | Entrega futura |
| :--- | :--- |
| OAuth orgânico (continuação) | Callback, cofre AES-256, rotas HTTP, refresh/revoke (Subfases 2.2–2.3) |
| IA / copiloto | Brand Voice, planner editorial, legendas (Fase 3) |
| Worker | Outbox, `SocialPublishingDispatchWorker`, kill switch (Fase 4) |
| Blazor | Calendário, portal `/approve/{token}` (Fase 5) |
| Sinergia paga | Boost e Creative Hub (Fase 6) |

---

## 8. Composição na Web API

Registro em `Program.cs`:

```csharp
builder.Services.AddSocialPublishingModule(builder.Configuration);
builder.Services.AddSocialPublishingTenantPersistenceComposition();
```

`AddSocialPublishingModule` registra MediatR, repositórios, fábrica de `SocialPublishingDbContext` e `ISocialPublishingSchemaMigrator`. `AddSocialPublishingTenantPersistenceComposition` decora `ITenantProvisioningService` e registra o hosted service de migração social para tenants existentes.
