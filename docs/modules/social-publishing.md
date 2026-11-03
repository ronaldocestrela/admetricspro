# Especificação Funcional & Técnica: Social Publishing (AI Social Copilot & Scheduling)

## 1. Visão geral

O bounded context **SocialPublishing** cobre gestão e publicação orgânica na plataforma AdMetricsPro: conexão com Meta (Instagram Professional/Business e Facebook Pages) e TikTok Content Posting API, geração de cronograma e criativos com IA, calendário com aprovação white-label e sugestão de impulsionamento quando um post orgânico ganha tração.

Até a **Subfase 1.2** o módulo inclui o **modelo de domínio puro** (agregados, enums, value objects e testes unitários), além dos projetos, conformidade arquitetural, composição na Web API (`AddSocialPublishingModule`) e ADR 0043. Persistência, OAuth, rotas HTTP de negócio e UI Blazor permanecem fora desta entrega.

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

## 5. Fora de escopo (após Subfase 1.2)

| Área | Entrega futura |
| :--- | :--- |
| Persistência | `SocialPublishingDbContext`, schema `social`, repositórios, migrações (Subfase 1.3) |
| HTTP | `GET /api/v1/social/status`, permissão `social.read` (Subfase 1.4) |
| OAuth orgânico | Authorize/callback, cofre AES-256 (Fase 2) |
| IA / copiloto | Brand Voice, planner editorial, legendas (Fase 3) |
| Worker | Outbox, `SocialPublishingDispatchWorker`, kill switch (Fase 4) |
| Blazor | Calendário, portal `/approve/{token}` (Fase 5) |
| Sinergia paga | Boost e Creative Hub (Fase 6) |

---

## 6. Composição na Web API

Registro único em `Program.cs`:

```csharp
builder.Services.AddSocialPublishingModule(builder.Configuration);
```

A extensão `AddSocialPublishingModule` (em `SocialPublishing.Infrastructure`) registra MediatR e o pipeline `ValidationBehavior` via `AddMessaging` sobre o assembly `SocialPublishing.Application`.
