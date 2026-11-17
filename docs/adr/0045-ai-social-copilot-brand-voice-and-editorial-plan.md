# 0045 — AI Social Copilot: Brand Voice, planner editorial e geração de cópia

## Status

Aceito em 2026-11-03. Subfases 3.1–3.4 implementadas (Brand Voice HTTP, `EditorialCalendarPlanner`, `GenerateSlotCopyCommand` + `HttpSocialCreativeGenerator`, materialização idempotente em `OrganicPost` Draft).

## Contexto

O módulo **SocialPublishing** precisa separar o que é **determinístico** (pesos de pilares, distribuição de slots, conversão de fuso) do que depende de **modelo de linguagem** (legendas e hashtags). As Subfases 3.1 e 3.2 já persistem o DNA de marca e produzem `EditorialSlot` sem texto. A Subfase 3.3 deve gerar cópia por rede/formato sem acoplar o domínio a um provedor específico, sem vazar tokens OAuth no prompt e sem persistir posts até a materialização do plano (Subfase 3.4).

Requisitos:

1. Brief montado apenas com `BrandVoiceProfile` + dados do slot (pilar, rede, formato).
2. Validação pós-IA com regras já existentes (`CaptionLimits`, `HashtagGroup`, termos proibidos).
3. Provedor substituível via porta na Application; adaptador HTTP na Infrastructure.
4. Falhas de rede/provedor como `Result.Failure`, sem exceção propagada ao handler MediatR.
5. Configuração sensível via `SocialAi` e variáveis de ambiente (ADR 0018).

## Decisão

### Subfases 3.1–3.2 (pré-requisitos)

- Brand Voice: `PUT` / `GET` `/api/v1/social/brand-voice/{workspaceId}`.
- Planner puro: `EditorialCalendarPlanner.Plan` no domínio, sem IA.

### Subfase 3.3 (implementada)

1. **`CopyBriefBuilder`** e **`CopyFormatInstructions`**: instruções distintas por rede/formato (carrossel, Reel/TikTok, feed Instagram vs Facebook Page).

2. **`ISocialCreativeGenerator`** na Application; **`HttpSocialCreativeGenerator`** na Infrastructure (`POST …/chat/completions`, JSON de resposta com `caption` e `hashtags.{brand,niche,reach}`).

3. **`CopyAcceptanceGate`**: termos proibidos, teto de legenda, `HashtagGroup.Create` sobre a união dos três grupos; **`warnings`** para links obrigatórios ausentes (não bloqueante).

4. **`GenerateSlotCopyCommand`**: MediatR + `social.copilot.generate`; **sem rota HTTP** nesta subfase.

5. **`SocialAiOptions`**: `BaseUrl`, `Model`, `ApiKey`, `Temperature`, `MaxTokens`, `TimeoutSeconds`. Binding em `AddSocialPublishingModule`; `SOCIAL_AI_*` opcionais. Sem `ValidateOnStart` — o host sobe sem chave; chamadas falham com `Social.AiConfigurationMissing`.

6. Erros estáveis: `Social.CopyContainsForbiddenTerm`, `Social.CopyGenerationFailed`, `Social.AiConfigurationMissing`.

### Subfase 3.4 (implementada)

1. **`GenerateEditorialPlanCommand`**: planner + cópia por slot via **`ISender`** (`GenerateSlotCopyCommand`); **`CommitAsync` por slot**; resultado parcial em **`GeneratedPlanDto`** (`createdPostIds`, `failedSlots`).

2. **`OrganicPost.CreateEditorialDraft`**: rascunhos editoriais **sem exigir mídia** (Reel, carrossel e TikTok nascem vazios até anexação na UI). Publicação continua bloqueada na Fase 4 sem mídia válida.

3. **Idempotência**: agregado **`EditorialPlanGeneration`** em `social.EditorialPlanGenerations`, chave **`workspaceId + rangeStart + rangeEnd + seed`**. Replay devolve snapshot **`Completed`**; corrida com registro **`InProgress`** → `Social.PlanGenerationInProgress` (409).

4. **HTTP**: `POST /api/v1/social/plans:generate`, `GET /api/v1/social/posts?workspaceId&from&to` (UTC inclusivo).

5. **Legenda persistida**: `EditorialCaptionComposer` concatena legenda aceita e hashtags; validação final contra `CaptionLimits`.

## Consequências

### Positivas

- Domínio e Application permanecem testáveis com gerador falso; planner e cópia desacoplados.
- Política de hashtags e legendas centralizada no gate, alinhada a `OrganicPost` na materialização futura.
- Segredos de IA fora do repositório Git (`.env.example` documentado).

### Negativas / Mitigações

- Dependência de formato JSON do modelo — mitigado por prompt estrito e testes do adaptador com `TestHttpMessageHandler`.
- Falhas de IA no meio do plano exigem disciplina de UX (lista `failedSlots`) — mitigado por sucesso parcial explícito e idempotência para retentativa manual de slots.
