# 0051 — Geração de imagem pós-aprovação com Gemini Nano Banana

## Status

Aceito em 2026-11-25.

## Contexto

O **SocialPublishing** já gera legendas via `ISocialCreativeGenerator` (API compatível com OpenAI) e permite aprovação white-label. Rascunhos editoriais nascem **sem mídia** (`OrganicPost.CreateEditorialDraft`, ADR 0045). Após o cliente aprovar, a agência precisa anexar arte de feed sem sair do calendário Blazor.

Requisitos:

1. Geração **somente sob demanda** da agência, com post em `Approved` e formato `FeedImage`.
2. Brief montado no servidor (Brand Voice + legenda aprovada); sem prompt cru na UI.
3. Provedor substituível; segredos fora do Git (ADR 0018).
4. Binário em `IOrganicMediaStore`; SQL guarda metadados e `ObjectKey`.
5. `Result<T>` em toda a orquestração; frontend só via HTTP.

## Decisão

1. **`ISocialImageGenerator`** na Application; **`HttpGeminiImageGenerator`** na Infrastructure chama a **Interactions API** do Gemini (`gemini-nano-banana-2.1`, `response_format` com `aspect_ratio` e `image_size`).

2. **`SocialImageAiOptions`** separado de `SocialAiOptions` (legendas).

3. **`ImageBriefBuilder`** reutiliza Brand Voice e remove hashtags da legenda antes do prompt. Proporção: Instagram `4:5`, Facebook Page `1:1`.

4. **`OrganicPost.ReplaceGeneratedImage`**: só `Approved` + `FeedImage`; substitui a coleção `_media` por um único `OrganicMediaAsset`.

5. **`GeneratePostImageCommand`** + `POST /api/v1/social/posts/{id}:generate-image` com permissão `social.copilot.generate`.

6. **`OrganicPostDto`** passa a expor `Media[]` com `PublicUrl` quando `OrganicMediaOptions.PublicBaseUrl` está configurado.

7. Erros estáveis: `Social.ImageGenerationFailed`, `Social.ImageAiConfigurationMissing`, `Social.ImageGenerationFormatUnsupported`, `Social.ImageGenerationDirectionTooLong`.

## Consequências

### Positivas

- Separação clara entre cópia (LLM chat) e imagem (Nano Banana).
- Fluxo alinhado à máquina de estados existente; publicação e outbox inalterados.
- Testes do adaptador com `TestHttpMessageHandler` sem internet.

### Negativas / Mitigações

- Custo por imagem e dependência do Google — mitigado por geração explícita (não automática) e configuração opcional.
- SintID obrigatório nas imagens — aviso na UI e na spec do módulo.
- Carrossel, Reel e TikTok ficam fora desta entrega — código `Social.ImageGenerationFormatUnsupported`.