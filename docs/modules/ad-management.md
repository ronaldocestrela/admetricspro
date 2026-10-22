# Gerenciamento de anúncios

## Rotas de apresentação

| Rota | Comportamento |
| --- | --- |
| `/ad-management` | Central com workspace e cards Meta (Facebook/Instagram), Google, TikTok e Microsoft/Bing. |
| `/ad-management/{workspaceId}/meta` | Área Meta dedicada com seleção de conta, sincronização, campanhas, conjuntos, anúncios, criativos e desempenho. |
| `/ad-management/{workspaceId}/{google,tiktok,bing}` | Área da plataforma com contas reais vinculadas, importação e campanhas da conta escolhida. |
| `/workspaces/{workspaceId}/meta` | Endereço anterior da Meta, preservado. |

O menu mantém Integrações de Anúncios para autorizações e adiciona Gerenciamento de Anúncios. Sem cliente, solicita cadastro de workspace. Os vínculos demonstrativos não habilitam gerenciamento real. O carregamento descarta respostas de contas/workspaces anteriores. As áreas Google/TikTok/Bing utilizam os conectores e operações existentes; não simulam conexões ausentes nem oferecem os formulários de criação exclusivos da Meta.

## API consumida

Headers: `Authorization: Bearer <JWT atual>` e `X-Tenant-Id: <tenant autenticado>`. O cliente consulta a sessão a cada requisição e acompanha troca de token/logout. Dados são consumidos exclusivamente via HttpClient tipado; o frontend não usa banco, repositórios ou mediador de persistência.

- `GET /api/v1/integrations/connected-accounts?workspaceId={id}`: vínculos.
- `GET /api/v1/integrations/campaigns?workspaceId={id}&platform=GoogleAds`: campanhas da plataforma, filtradas também por conta na apresentação.
- `POST /api/v1/integrations/campaigns/sync`: hierarquia.
- `POST /api/v1/integrations/campaigns/metrics/sync`: métricas, somente após sucesso da hierarquia.

Payload de hierarquia:

```json
{ "workspaceId": "11111111-1111-1111-1111-111111111111", "connectedAdAccountId": "22222222-2222-2222-2222-222222222222", "platform": "GoogleAds" }
```

Para métricas, acrescentar `startDateUtc`, `endDateUtc` (dias UTC inclusivos) e `granularity: "Daily"`.

Retorno de hierarquia:

```json
{ "isSuccess": true, "isFailure": false, "error": { "code": "", "description": "", "type": 0 }, "value": { "totalAccountsProcessed": 1, "totalCampaignsSynced": 2, "totalAdSetsSynced": 3, "totalAdsSynced": 4, "syncedAtUtc": "2026-10-22T12:00:00Z", "syncedPlatforms": ["GoogleAds"] } }
```

Erros: Sync.InvalidRequest para conta/workspace vazio, plataforma desconhecida ou datas invertidas; Auth.Unauthorized para sessão ausente/expirada. Falhas de autorização, credencial, conector e importação são propagadas por Result. Falha de hierarquia não dispara métricas. Sincronização disponível depende da autorização e implementação de cada conector.
