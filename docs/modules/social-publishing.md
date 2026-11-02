# Especificação Funcional & Técnica: Social Publishing (AI Social Copilot & Scheduling)

## 1. Visão geral

O bounded context **SocialPublishing** cobre gestão e publicação orgânica na plataforma AdMetricsPro: conexão com Meta (Instagram Professional/Business e Facebook Pages) e TikTok Content Posting API, geração de cronograma e criativos com IA, calendário com aprovação white-label e sugestão de impulsionamento quando um post orgânico ganha tração.

Nesta **Subfase 1.1** existem apenas os projetos do módulo, testes de conformidade arquitetural, registro de composição na Web API (`AddSocialPublishingModule`) e governança (ADR 0043). Não há agregados, persistência, OAuth, rotas HTTP nem UI Blazor.

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

## 4. Fora de escopo desta fase (Subfase 1.1)

Os itens abaixo pertencem às subfases seguintes do roadmap e **não** fazem parte desta entrega:

| Área | Entrega futura |
| :--- | :--- |
| Modelo de domínio | Agregados `OrganicConnection`, `OrganicPost`, `BrandVoiceProfile`, etc. (Subfase 1.2) |
| Persistência | `SocialPublishingDbContext`, schema `social`, repositórios, migrações (Subfase 1.3) |
| HTTP | `GET /api/v1/social/status`, permissão `social.read` (Subfase 1.4) |
| OAuth orgânico | Authorize/callback, cofre AES-256 (Fase 2) |
| IA / copiloto | Brand Voice, planner editorial, legendas (Fase 3) |
| Worker | Outbox, `SocialPublishingDispatchWorker`, kill switch (Fase 4) |
| Blazor | Calendário, portal `/approve/{token}` (Fase 5) |
| Sinergia paga | Boost e Creative Hub (Fase 6) |

---

## 5. Composição na Web API

Registro único em `Program.cs`:

```csharp
builder.Services.AddSocialPublishingModule(builder.Configuration);
```

A extensão `AddSocialPublishingModule` (em `SocialPublishing.Infrastructure`) registra MediatR e o pipeline `ValidationBehavior` via `AddMessaging` sobre o assembly `SocialPublishing.Application`.
