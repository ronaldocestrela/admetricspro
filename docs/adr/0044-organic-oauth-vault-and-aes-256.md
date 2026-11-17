# 0044 — OAuth orgânico, state anti-CSRF e cofre AES-256 separado

## Status

Aceito em 2026-11-02. Subfase 2.2 implementada em 2026-11-03.

## Contexto

O bounded context **SocialPublishing** conecta Instagram Professional/Business, Facebook Pages e TikTok Content Posting com escopos distintos do hub de mídia paga (ADR 0026). Reutilizar `OAuthTokenVault`, `IOAuthStateService` ou `Integrations.Infrastructure` violaria os guardrails do ADR 0043 (sem referência cruzada a Integrations) e misturaria tokens de anúncios com tokens de publicação orgânica.

Requisitos da Fase 2:

1. State OAuth assinado (HMAC-SHA256) com `TenantId`, `WorkspaceId`, plataforma orgânica, `RedirectUri` e TTL de 15 minutos, sem transportar access token.
2. URLs de consentimento por rede, com adaptadores distintos para Instagram, Facebook Page e TikTok Content Posting.
3. Persistência de tokens cifrados com `IEncryptionService` (AES-256-CBC do kernel) e envelope de propósito `organic:{platform}:{workspaceId}:{connectionId}` — **não** o cofre de anúncios.

## Decisão

### Subfase 2.1 (implementada)

1. **`OrganicOAuthOptions`** na Application, seção `SocialPublishing:OAuth`, com **`StateSigningKey` própria** (mínimo 32 caracteres, sem fallback aleatório em produção). Não reutiliza `Integrations:OAuth:StateSigningKey`.

2. **`OrganicOAuthStateService`**: payload JSON em Base64Url + assinatura HMAC-SHA256 em Base64Url; campos `TenantId`, `WorkspaceId`, `OrganicPlatform`, `RedirectUri`, `IssuedAtUtc`, `Nonce`. Falhas de formato, adulteração, expiração ou mismatch tenant/workspace retornam **`Social.OAuthStateInvalid`** (mesma resposta em todos os casos).

3. **`IOrganicPlatformAdapter`**: `MetaInstagramOrganicAdapter`, `MetaFacebookPageOrganicAdapter` (host `https://www.facebook.com/{versão}/dialog/oauth`, escopos distintos) e `TikTokContentPostingAdapter` (Login Kit `https://www.tiktok.com/v2/auth/authorize/`). `BuildAuthorizationUrl` não realiza HTTP.

4. **`StartOrganicAuthorizationQuery`**: emite state e devolve `StartOrganicAuthorizationResponse` (`authorizationUrl`, `state`, `expiresAtUtc`).

5. **Credenciais Meta** podem reutilizar `META_APP_ID` / `META_APP_SECRET` / `META_GRAPH_API_VERSION` via binding na composição; **TikTok orgânico** usa `SocialPublishing:OAuth:TikTok:ClientKey` separado da Marketing API.

### Subfase 2.2 (implementada)

1. **`OrganicTokenProtector`**: serializa `{ purpose, secret }`, cifra com **`IEncryptionService`** do kernel (AES-256-CBC). O propósito autenticado **não** altera o algoritmo do kernel; fica no envelope JSON. Decifrar com propósito divergente → **`Social.VaultPurposeMismatch`**.

2. **`CompleteOrganicAuthorizationCommand`**: valida state **antes** de `ExchangeCodeAsync`; persiste ciphertext em `OrganicConnection`; reconexão da mesma conta externa usa `Reauthorize` (mesmo `ConnectionId`).

3. **Rotas HTTP** `POST /api/v1/social/connections/authorize` e `POST /api/v1/social/connections/callback` com permissão **`TenantPermission.SocialConnectionsManage`** (`social.connections.manage`).

4. **State continua stateless** (sem tabela de nonce consumido nesta entrega); single-use do authorization code fica a cargo do provedor OAuth.

5. **HttpClient** dos adaptadores orgânicos registrado com `RemoveAllLoggers()`; handlers não registram código, client_secret nem tokens em log.

## Consequências

### Positivas

- Isolamento total do vault e state de anúncios; escopos orgânicos não aparecem no módulo Integrations.
- Testes unitários cobrem state, troca simulada e cofre sem internet (`TestHttpMessageHandler`).
- Alinhamento com `Result<T>` e limites do ADR 0043.

### Negativas / mitigações

- Duplicação controlada do padrão HMAC (similar ao ADR 0026) — mitigada por assembly separado e chave distinta.
- Operadores devem configurar `SocialPublishing__OAuth__StateSigningKey` além das chaves de Integrations — documentado em `.env.example` e validação `ValidateOnStart`.
- Refresh, revoke e listagem HTTP seguem na Subfase 2.3.
