# Módulo Integrations — Hub de Autenticação OAuth2 e Token Vault

Este documento especifica a arquitetura técnica, fluxos de autorização, contratos de API, escopos requeridos e estratégias de segurança para a integração com as 4 redes de anúncios (**Meta Ads**, **Google Ads**, **Bing Ads** e **TikTok Ads**), com armazenamento seguro das credenciais no **Token Vault** criptografado com **AES-256** no banco de dados dedicado do inquilino.

---

## 1. Visão Geral e Arquitetura

O Hub de Integrações OAuth2 unifica a experiência de conexão com plataformas de tráfego pago para os workspaces das agências, operando como um módulo independente no monólito modular:

1. **Desacoplamento de Redes Externas:**
   - Adaptadores especializados (`MetaAdsOAuthAdapter`, `GoogleAdsOAuthAdapter`, `BingAdsOAuthAdapter`, `TikTokAdsOAuthAdapter`) implementam o contrato universal `IOAuthAdapter`.
   - O serviço `IAdNetworkAuthService` orquestra dinamicamente as chamadas de autorização, troca de código e renovação.
2. **Proteção Anti-CSRF via State Assinado:**
   - O `OAuthStateService` gera tokens de estado HMAC-SHA256 contendo `TenantId`, `WorkspaceId`, `Platform`, `RedirectUri` e carimbo UTC com expiração curta (TTL de 15 minutos).
3. **Cofre de Credenciais (Token Vault):**
   - Os tokens sensíveis (`access_token` e `refresh_token`) nunca são expostos em texto claro.
   - São criptografados com **AES-256-CBC** com IV aleatório exclusivo para cada registro via `IEncryptionService` antes de serem gravados na tabela `OAuthTokenVaults` do banco operacional do tenant.
4. **Renovação Preventiva de Tokens:**
   - Tokens com expiração próxima (< 72 horas) podem ser renovados em lote através do endpoint `/api/v1/integrations/oauth/refresh` ou jobs programados em segundo plano.

---

## 2. Escopos e Endpoints por Plataforma de Anúncios

| Plataforma | Endpoints OAuth2 | Escopos Mandatórios | Duração do Token |
| :--- | :--- | :--- | :--- |
| **Meta Ads** | Auth: `https://www.facebook.com/v21.0/dialog/oauth`<br>Token: `https://graph.facebook.com/v21.0/oauth/access_token` | `ads_read`, `ads_management`, `read_insights`, `business_management` | 60 dias (após troca por *long-lived token*) |
| **Google Ads** | Auth: `https://accounts.google.com/o/oauth2/v2/auth`<br>Token: `https://oauth2.googleapis.com/token` | `https://www.googleapis.com/auth/adwords`<br>(com `access_type=offline` e `prompt=consent`) | Access: 1 hora<br>Refresh: Contínuo (até revogação) |
| **Bing Ads** | Auth: `https://login.microsoftonline.com/common/oauth2/v2.0/authorize`<br>Token: `https://login.microsoftonline.com/common/oauth2/v2.0/token` | `https://ads.microsoft.com/msads.manage`, `offline_access` | Access: 1 hora<br>Refresh: 90 dias |
| **TikTok Ads** | Auth: `https://business-api.tiktok.com/portal/auth`<br>Token: `https://business-api.tiktok.com/open_api/v1.3/oauth2/access_token/` | `user.info.basic`, `video.list`, `advertiser.insights` | Access: 24 horas<br>Refresh: 365 dias |

---

## 3. Especificação dos Endpoints RESTful (Web API)

### 3.1 Iniciar Fluxo de Autorização
- **Rota:** `GET /api/v1/integrations/oauth/authorize-url`
- **Sumário OpenAPI:** `Inicia fluxo de autorização OAuth2 com plataforma de anúncios`
- **Parâmetros de Consulta:**
  - `workspaceId` (Guid, obrigatório): Workspace ao qual vincular a conta.
  - `platform` (string, obrigatório): `MetaAds`, `GoogleAds`, `TikTokAds` ou `BingAds`.
  - `redirectUri` (string, obrigatório): URL de callback registrada no provedor.

#### Exemplo de Resposta de Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": {
    "authorizationUrl": "https://www.facebook.com/v21.0/dialog/oauth?client_id=12345&redirect_uri=https%3A%2F%2Fapp.admetricspro.com%2Fcallback&state=eyJUZW5hbnRJZCI6...&scope=ads_read%2Cads_management",
    "state": "eyJUZW5hbnRJZCI6...signature"
  }
}
```

---

### 3.2 Processar Callback de Autorização
- **Rota:** `GET /api/v1/integrations/oauth/callback`
- **Sumário OpenAPI:** `Processa o callback de autorização OAuth2 e armazena credenciais no Token Vault`
- **Parâmetros de Consulta:**
  - `code` (string): Código retornado pelo consent screen.
  - `state` (string): Token de estado recebido de volta.
  - `redirectUri` (string): Mesma URI utilizada no request inicial.

#### Exemplo de Resposta de Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": {
    "id": "b7d2f928-1111-4444-9999-000000000001",
    "workspaceId": "c8a3e742-2222-4444-8888-000000000002",
    "platform": "MetaAds",
    "externalAccountId": "act_1020304050",
    "externalAccountName": "Agência de Performance Alfa",
    "status": "Active",
    "scopes": "ads_read,ads_management,read_insights",
    "accessTokenExpiresAtUtc": "2026-11-14T19:30:00Z",
    "isExpiringSoon": false,
    "createdAtUtc": "2026-09-15T19:30:00Z",
    "updatedAtUtc": null
  }
}
```

---

### 3.3 Renovação Preventiva de Tokens
- **Rota:** `POST /api/v1/integrations/oauth/refresh?thresholdHours=72`
- **Sumário OpenAPI:** `Executa renovação preventiva de tokens de acesso prestes a expirar`

#### Exemplo de Resposta (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": 4
}
```

---

### 3.4 Listar Conexões por Workspace
- **Rota:** `GET /api/v1/integrations/oauth/status/{workspaceId}`
- **Sumário OpenAPI:** `Obtém o status de todas as conexões OAuth vinculadas a um workspace`

#### Exemplo de Resposta (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": [
    {
      "id": "b7d2f928-1111-4444-9999-000000000001",
      "workspaceId": "c8a3e742-2222-4444-8888-000000000002",
      "platform": "MetaAds",
      "externalAccountId": "act_1020304050",
      "externalAccountName": "Meta Ads Principal",
      "status": "Active",
      "scopes": "ads_read,ads_management",
      "accessTokenExpiresAtUtc": "2026-11-14T19:30:00Z",
      "isExpiringSoon": false,
      "createdAtUtc": "2026-09-15T19:30:00Z",
      "updatedAtUtc": null
    },
    {
      "id": "a1b2c3d4-3333-4444-7777-000000000003",
      "workspaceId": "c8a3e742-2222-4444-8888-000000000002",
      "platform": "GoogleAds",
      "externalAccountId": "customers/1234567890",
      "externalAccountName": "Google Ads Leads",
      "status": "Active",
      "scopes": "https://www.googleapis.com/auth/adwords",
      "accessTokenExpiresAtUtc": "2026-09-15T20:30:00Z",
      "isExpiringSoon": true,
      "createdAtUtc": "2026-09-15T19:30:00Z",
      "updatedAtUtc": null
    }
  ]
}
```

---

### 3.5 Revogar Conexão
- **Rota:** `DELETE /api/v1/integrations/oauth/{workspaceId}/{platform}`
- **Sumário OpenAPI:** `Revoga uma conexão OAuth2 e desativa as credenciais no Token Vault`

#### Exemplo de Resposta (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 }
}
```

---

## 4. Segurança em Repouso (Token Vault)

- A entidade `OAuthTokenVault` armazena `EncryptedAccessToken` e `EncryptedRefreshToken`.
- Criptografia executada com algoritmo **AES-256-CBC** e vetor de inicialização criptograficamente aleatório (16 bytes) gerado para cada gravação.
- O ciphertext persistido possui o formato `Convert.ToBase64String(IV + CipherBytes)`.
- A chave de criptografia de 32 bytes é injetada externamente e isolada das camadas de apresentação e persistência física.

---

## 5. Códigos de Erro de Negócio Mapeados

| Código de Erro | Motivo | Status HTTP Sugerido |
| :--- | :--- | :--- |
| `OAuthState.InvalidSignature` | Estado adulterado ou assinatura HMAC não coincide | `422 Unprocessable Entity` |
| `OAuthState.Expired` | Callback recebido após a janela de validade de 15 minutos | `422 Unprocessable Entity` |
| `OAuthState.InvalidFormat` | State nulo, vazio ou malformado | `400 BadRequest` |
| `AdNetworkAuth.UnsupportedPlatform` | Plataforma não suportada ou não registrada | `400 BadRequest` |
| `MetaAds.OAuthFailed` | Falha na troca de código na Meta Graph API | `400 BadRequest` |
| `GoogleAds.OAuthFailed` | Falha ou autorização revogada na Google Ads API | `400 BadRequest` |
| `BingAds.OAuthFailed` | Falha no Microsoft Identity Platform | `400 BadRequest` |
| `TikTokAds.OAuthFailed` | Falha de autenticação na TikTok Marketing API | `400 BadRequest` |
| `RevokeOAuth.NotFound` | Conexão para a plataforma informada não encontrada no workspace | `404 NotFound` |

---

## 6. Interface Frontend Blazor Server (`/integrations`)

A gestão operacional das conexões OAuth2 é exposta no frontend através da página `IntegrationsPage.razor`:

- **Rotas:** `/integrations` e `/workspaces/{WorkspaceId:guid}/integrations`
- **Seletor de Workspace:** Permite alternar o contexto de cliente, sincronizado com `IWorkspaceContextStateProvider`.
- **KPIs em Tempo Real:** Total de canais oficiais suportados (4), conexões ativas, tokens em risco (< 72h) e canais desconectados.
- **Ações Disponíveis por Plataforma:**
  - *Conectar OAuth:* Invoca `IOAuthIntegrationsClientService.InitiateOAuthFlowAsync` e redireciona o usuário à tela de consentimento da rede.
  - *Modo Demonstração (FTUX):* Invoca `ITenantFtuxClientService.ConnectDemoAccountAsync` para gerar contas simuladas com dados analíticos instantâneos.
  - *Desconectar / Revogar:* Modal de confirmação que remove a credencial do Token Vault via `RevokeConnectionAsync`.
- **Ação Global de Renovação Preventiva:** Botão no cabeçalho disparando `RefreshExpiringTokensAsync` com feedback de contagem de tokens atualizados.
- **Garantia de Não Acesso a Banco:** Consumo exclusivo via Web API através do cliente fortemente tipado `IOAuthIntegrationsClientService`.
- **Resiliência a Recarregamento (F5) e Ciclo de Vida do Blazor Server:**
  - Assinatura reativa dos eventos `ITenantSessionStateProvider.OnSessionChanged` e `IWorkspaceContextStateProvider.OnWorkspaceChanged` com implementação estrita de `IDisposable`.
  - Tratamento no `OnAfterRenderAsync(firstRender: true)` para restauração assíncrona da sessão e do workspace ativo via `ProtectedLocalStorage` quando o componente é instanciado em um novo circuito SignalR.
  - Recarregamento automático de workspaces e conexões de mídia tão logo a identidade do tenant seja restabelecida, evitando exibição indevida do estado vazio (*Empty State*) ou perda do contexto de cliente.


