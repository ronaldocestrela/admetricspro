# Módulo Integrations — Pipeline de Ingestão de Métricas Diárias e Horárias & Idempotência

Este documento especifica os contratos de dados, regras de agregação, chave composta de unicidade e consumo da Web API para o **Pipeline de Ingestão de Métricas de Desempenho** (Meta Ads, Google Ads, TikTok Ads e Bing Ads) no modo **Database-per-Tenant**.

---

## 1. Visão Geral e Princípios Fundamentais

O pipeline consolida números brutos e calculados de veiculação em duas granularidades temporais:
1. **Diária (`Daily`):** Agrupamento por 24 horas (data normalizada à meia-noite UTC).
2. **Horária (`Hourly`):** Agrupamento por hora do dia (0 a 23) para análise de curva de pico comercial e pacing intradia.

### Garantia de Idempotência Estrita (Item 2.3.1)
Múltiplas execuções da sincronização para o mesmo intervalo de tempo **nunca criam linhas duplicadas**. A persistência utiliza o padrão *Upsert* atômico em lote coordenado por uma chave natural única composta no banco do inquilino:

$$\text{Chave Composta} = (\text{ConnectedAdAccountId}, \text{ExternalCampaignId}, \text{ExternalAdSetId}, \text{ExternalAdId}, \text{Date}, \text{Hour}, \text{Granularity})$$

Se a rede atualizar dados retroativos (como conversões tardias que chegam em até 72 horas), a rotina atualiza os campos (`Spend`, `Impressions`, `Clicks`, `Conversions`, `ConversionValue`) e recalcula os KPIs derivados automaticamente.

---

## 2. Modelo de Dados e KPIs Derivados

### 2.1 Entidade `CampaignMetric`
| Campo | Tipo | Descrição |
| :--- | :--- | :--- |
| `Id` | `Guid` | Chave primária do registro analítico. |
| `WorkspaceId` | `Guid` | Identificador do workspace / cliente da agência. |
| `ConnectedAdAccountId`| `Guid` | Identificador da conta conectada de origem. |
| `CampaignId` | `Guid` | Chave estrangeira referenciando a `Campaign` local. |
| `AdSetId` | `Guid?` | Identificador opcional do conjunto ou grupo. |
| `AdId` | `Guid?` | Identificador opcional do criativo / anúncio. |
| `Platform` | `string` | Rede de anúncios (`MetaAds`, `GoogleAds`, `TikTokAds`, `BingAds`). |
| `ExternalCampaignId` | `string` | ID externo da campanha na rede. |
| `Date` | `DateTime` | Data da métrica normalizada em UTC (meia-noite). |
| `Hour` | `int?` | Hora do dia (0 a 23) para métricas horárias; nulo para diárias. |
| `Granularity` | `MetricGranularity`| Enum: `Daily = 1`, `Hourly = 2`. |
| `Spend` | `decimal(18,4)`| Investimento bruto incorrido na moeda local da conta. |
| `Currency` | `string(10)` | Código ISO 4217 da moeda (ex: `BRL`, `USD`). |
| `Impressions` | `long` | Total de visualizações veiculadas. |
| `Clicks` | `long` | Total de cliques válidos computados. |
| `Conversions` | `decimal(18,4)`| Total de conversões (suporta decimais para atribuição data-driven). |
| `ConversionValue` | `decimal(18,4)`| Receita financeira direta gerada por conversões. |

### 2.2 Fórmulas dos KPIs Calculados em Tempo Real
- **CTR (*Click-Through Rate*):** $(\text{Clicks} \div \text{Impressions}) \times 100\%$
- **CPC (*Cost Per Click*):** $\text{Spend} \div \text{Clicks}$
- **CPM (*Cost Per Mille*):** $(\text{Spend} \div \text{Impressions}) \times 1000$
- **CPA (*Cost Per Action*):** $\text{Spend} \div \text{Conversions}$
- **ROAS (*Return On Ad Spend*):** $\text{ConversionValue} \div \text{Spend}$

---

## 3. Especificação dos Endpoints RESTful Web API

### 3.1 Disparar Ingestão de Métricas
- **Rota:** `POST /api/v1/integrations/campaigns/metrics/sync`
- **Sumário OpenAPI:** `Dispara a ingestão de métricas diárias e horárias de campanhas de um workspace`
- **Headers:** `X-Tenant-Id: {guid}`
- **Corpo da Requisição (JSON):**
  ```json
  {
    "workspaceId": "c8a3e742-2222-4444-8888-000000000002",
    "connectedAdAccountId": null,
    "platform": "MetaAds",
    "startDateUtc": "2026-09-01T00:00:00Z",
    "endDateUtc": "2026-09-15T00:00:00Z",
    "granularity": "Daily"
  }
  ```
- **Resposta de Sucesso (`200 OK`):**
  ```json
  {
    "isSuccess": true,
    "isFailure": false,
    "error": { "code": "", "description": "", "type": 0 },
    "value": {
      "totalAccountsProcessed": 1,
      "totalRecordsIngested": 15,
      "totalSpend": 4500.50,
      "totalImpressions": 120500,
      "totalClicks": 3420,
      "totalConversions": 142.0,
      "totalConversionValue": 18500.00,
      "syncedAtUtc": "2026-09-15T23:45:00Z",
      "granularity": "Daily",
      "syncedPlatforms": [
        "MetaAds"
      ]
    }
  }
  ```

---

### 3.2 Consultar Métricas e KPIs
- **Rota:** `GET /api/v1/integrations/campaigns/metrics`
- **Sumário OpenAPI:** `Consulta métricas analíticas de desempenho de campanhas por período e granularidade`
- **Parâmetros de Query:**
  - `workspaceId` (Guid, obrigatório)
  - `startDateUtc` (DateTime, opcional)
  - `endDateUtc` (DateTime, opcional)
  - `granularity` (string, opcional: "Daily" ou "Hourly")
  - `campaignId` (Guid, opcional)
  - `connectedAdAccountId` (Guid, opcional)
- **Resposta de Sucesso (`200 OK`):**
  ```json
  {
    "isSuccess": true,
    "isFailure": false,
    "error": { "code": "", "description": "", "type": 0 },
    "value": [
      {
        "id": "7bf34b12-9999-4444-1111-000000000001",
        "workspaceId": "c8a3e742-2222-4444-8888-000000000002",
        "connectedAdAccountId": "b7d2f928-1111-4444-9999-000000000001",
        "campaignId": "e45f9110-3333-4444-5555-000000000001",
        "adSetId": null,
        "adId": null,
        "platform": "MetaAds",
        "externalCampaignId": "23851029384",
        "externalAdSetId": null,
        "externalAdId": null,
        "date": "2026-09-15T00:00:00Z",
        "hour": null,
        "granularity": "Daily",
        "spend": 300.00,
        "currency": "BRL",
        "impressions": 10000,
        "clicks": 500,
        "conversions": 25.0,
        "conversionValue": 1500.00,
        "ctr": 5.0,
        "cpc": 0.60,
        "cpm": 30.00,
        "cpa": 12.00,
        "roas": 5.0,
        "syncedAtUtc": "2026-09-15T23:45:00Z"
      }
    ]
  }
  ```

---

## 4. Frontend Blazor: `ConnectionStatusList.razor`

Conforme a **Regra 9 do AGENTS.md**, o componente Blazor não acessa bancos ou tabelas diretamente. Ele consome o serviço cliente fortemente tipado `IOAuthIntegrationsClientService` (`OAuthIntegrationsClientService.cs`), invocando:
1. `GET /api/v1/integrations/oauth/status/{workspaceId}` para carregar badges de expiração e contas.
2. `POST /api/v1/integrations/oauth/refresh` para disparar a renovação preventiva sob demanda com feedback visual imediato e sem recarregar a página.
