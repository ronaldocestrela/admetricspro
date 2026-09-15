# Módulo Integrations — Sincronização Estrutural de Campanhas & Rate Limiting

Este documento especifica a arquitetura técnica, modelo unificado de hierarquia, conversão de payloads nativos das 4 redes externas (**Meta Ads**, **Google Ads**, **Bing Ads** e **TikTok Ads**), resiliência com backoff exponencial truncado e emissão de eventos in-memory.

---

## 1. Visão Geral e Arquitetura

A **Sincronização Estrutural de Campanhas** converte as estruturas heterogêneas dos gerenciadores de anúncios externos no modelo universal de 4 camadas:

$$\text{Conta Conectada (ConnectedAdAccount)} \longrightarrow \text{Campanha (Campaign)} \longrightarrow \text{Conjunto / Grupo (AdSet / AdGroup)} \longrightarrow \text{Anúncio / Criativo (Ad / Creative)}$$

### Princípios de Engenharia
1. **Normalização Semântica:** Mappers dedicados (`MetaAdsHierarchyMapper`, `GoogleAdsHierarchyMapper`, `TikTokAdsHierarchyMapper`, `BingAdsHierarchyMapper`) convertem formatos heterogêneos (centavos, micros, estados de operação) para tipos e enums fortemente tipados do C# (`CampaignStatus`, `AdSetStatus`, `AdStatus`, `AdCreativeType`).
2. **Resiliência e Rate Limiting:** A classe `HierarchyRateLimitPolicy` aplica política de retentativas com backoff exponencial truncado e jitter aleatório ($T = 2^{\text{attempt}} \times \text{baseDelay} + \text{jitter}$), detectando cabeçalhos e códigos de status 429 ou exaustão de quota sem lançar exceções.
3. **Isolamento Multitenant (Database-per-Tenant):** Toda a árvore hierárquica é persistida no `TenantDbContext` dedicado de cada inquilino através do repositório `ICampaignHierarchyRepository` com upsert atômico em lote (`UpsertHierarchyBatchAsync`).
4. **Comunicação Inter-Módulos In-Memory:** Ao concluir a sincronização de cada conta, o manipulador CQRS emite o evento in-memory `CampaignHierarchySyncedEvent` via `MediatR` / `IDomainEvent`.
5. **Aceleração FTUX com Modo Demonstração:** Para contas com `IsDemo == true`, o adaptador `DemoHierarchySyncAdapter` sintetiza uma hierarquia completa (Topo, Meio e Fundo de Funil) com criativos e URLs de destino realistas, dispensando credenciais ativas.

---

## 2. Modelo Universal de Dados

### 2.1 Mapeamento Comparativo entre Redes

| Conceito Universal | Meta Ads Graph API v21.0 | Google Ads API (GAQL) | TikTok Ads API v1.3 | Bing Ads API v13 |
| :--- | :--- | :--- | :--- | :--- |
| **Campanha** | `campaign` (`id`, `name`, `status`, `daily_budget`) | `campaign` (`id`, `name`, `status`, `amount_micros`) | `campaign` (`campaign_id`, `campaign_name`, `budget`) | `Campaign` (`Id`, `Name`, `Status`, `DailyBudget`) |
| **Conjunto / Grupo** | `adset` (`id`, `name`, `bid_strategy`, `targeting`) | `ad_group` (`id`, `name`, `status`, `type`) | `adgroup` (`adgroup_id`, `adgroup_name`, `bid_type`) | `AdGroup` (`Id`, `Name`, `Status`) |
| **Anúncio / Criativo** | `ad` (`id`, `name`, `creative`, `object_story_spec`) | `ad_group_ad` (`ad.id`, `ad.name`, `final_urls`, RSA) | `ad` (`ad_id`, `ad_name`, `ad_text`, `landing_page_url`) | `Ad` (`Id`, `Type`, `FinalUrls`, `Headlines`) |
| **Status Ativo** | `ACTIVE` | `ENABLED` | `ENABLE` | `Active` |
| **Status Pausado** | `PAUSED` | `PAUSED` | `DISABLE` | `Paused` |
| **Status Excluído** | `DELETED` / `ARCHIVED` | `REMOVED` | `DELETE` | `Deleted` |
| **Unidade Orçamentária** | Centavos (`cents / 100m`) | Micros (`micros / 1.000.000m`) | Decimal padrão | Decimal padrão |

---

## 3. Especificação dos Endpoints RESTful (Web API)

### 3.1 Disparar Sincronização Estrutural de Campanhas
- **Rota:** `POST /api/v1/integrations/campaigns/sync`
- **Sumário OpenAPI:** `Dispara a sincronização estrutural de campanhas de um workspace`
- **Parâmetros no Corpo (JSON):**
  - `workspaceId` (Guid, obrigatório): Identificador do workspace.
  - `connectedAdAccountId` (Guid, opcional): Filtro para sincronizar apenas uma conta específica.
  - `platform` (string, opcional): Filtro para sincronizar apenas uma plataforma (`MetaAds`, `GoogleAds`, etc.).

#### Exemplo de Requisição:
```json
{
  "workspaceId": "c8a3e742-2222-4444-8888-000000000002",
  "platform": "MetaAds"
}
```

#### Exemplo de Resposta de Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": {
    "totalAccountsProcessed": 1,
    "totalCampaignsSynced": 3,
    "totalAdSetsSynced": 4,
    "totalAdsSynced": 4,
    "syncedAtUtc": "2026-09-15T23:30:00Z",
    "syncedPlatforms": [
      "MetaAds"
    ]
  }
}
```

---

### 3.2 Consultar Hierarquia de Campanhas
- **Rota:** `GET /api/v1/integrations/campaigns`
- **Sumário OpenAPI:** `Obtém a estrutura completa de campanhas, conjuntos e anúncios de um workspace`
- **Parâmetros de Consulta:**
  - `workspaceId` (Guid, obrigatório): Identificador do workspace.
  - `connectedAdAccountId` (Guid, opcional): Filtrar por conta conectada.
  - `campaignId` (Guid, opcional): Filtrar por campanha específica.
  - `platform` (string, opcional): Filtrar por plataforma.
  - `status` (string, opcional): Filtrar por status (`Active`, `Paused`, etc.).

#### Exemplo de Resposta de Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": [
    {
      "id": "e45f9110-3333-4444-5555-000000000001",
      "workspaceId": "c8a3e742-2222-4444-8888-000000000002",
      "connectedAdAccountId": "b7d2f928-1111-4444-9999-000000000001",
      "platform": "MetaAds",
      "externalCampaignId": "23851029384",
      "name": "Campanha Conversão E-commerce",
      "status": "Active",
      "objective": "OUTCOME_SALES",
      "dailyBudget": 150.00,
      "lifetimeBudget": null,
      "currency": "BRL",
      "startDateUtc": "2026-03-01T00:00:00Z",
      "endDateUtc": null,
      "lastSyncedAtUtc": "2026-09-15T23:30:00Z",
      "adSets": [
        {
          "id": "f56a0221-4444-5555-6666-000000000002",
          "campaignId": "e45f9110-3333-4444-5555-000000000001",
          "externalAdSetId": "23851029390",
          "name": "Conjunto Lookalike 1%",
          "status": "Active",
          "bidStrategy": "LOWEST_COST_WITHOUT_CAP",
          "optimizationGoal": "OFFSITE_CONVERSIONS",
          "dailyBudget": 150.00,
          "lifetimeBudget": null,
          "targetingSummary": "{\"geo_locations\": {\"countries\": [\"BR\"]}}",
          "ads": [
            {
              "id": "a12b34cd-5555-6666-7777-000000000003",
              "adSetId": "f56a0221-4444-5555-6666-000000000002",
              "externalAdId": "23851029395",
              "name": "Criativo Oferta Especial - Imagem",
              "status": "Active",
              "creativeType": "Image",
              "headline": "Super Desconto de 50%",
              "body": "Compre agora antes que acabe o estoque.",
              "destinationUrl": "https://www.loja.com.br/promo",
              "previewUrl": "https://cdn.exemplo.com/ad1.jpg",
              "callToAction": "SHOP_NOW"
            }
          ]
        }
      ]
    }
  ]
}
```

---

## 4. Evento de Domínio In-Memory

Após a persistência bem-sucedida, o manipulador emite o evento in-memory:

```csharp
public sealed record CampaignHierarchySyncedEvent(
    Guid TenantId,
    Guid WorkspaceId,
    Guid ConnectedAdAccountId,
    string Platform,
    int CampaignsSynced,
    int AdSetsSynced,
    int AdsSynced,
    DateTime SyncedAtUtc) : IDomainEvent;
```

Módulos consumidores (`Analytics` e `Automations`) podem assinar este evento via `IDomainEventHandler<CampaignHierarchySyncedEvent>` sem acoplamento direto com a infraestrutura de integrações.
