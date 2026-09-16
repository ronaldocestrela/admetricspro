# Especificação Funcional & Técnica: Edição e Operações em Massa Multiplataforma

## 1. Visão Geral
O subsistema de **Edição e Operações em Massa Multiplataforma** permite que agências e gestores de tráfego apliquem mutações simultâneas (ativar, pausar ou reajustar orçamentos) em múltiplas campanhas conectadas de diferentes redes de anúncios (Meta Ads, Google Ads, TikTok Ads e Bing Ads) no contexto de um workspace.

A funcionalidade segue o padrão de **Tolerância a Falhas Parciais**, onde itens válidos do lote são persistidos e itens inválidos são devolvidos com mensagens claras de erro.

---

## 2. Casos de Uso & Regras de Negócio

### 2.1 Ações em Lote Disponíveis
* **Ativar (`Activate`):** Altera o status de campanhas pausadas para ativo (`CampaignStatus.Active`).
* **Pausar (`Pause`):** Altera o status de campanhas ativas para pausado (`CampaignStatus.Paused`).
* **Reajustar Orçamento (`AdjustBudget`):**
  * **Percentual (`PercentageChange`):** Aplica variação percentual sobre o orçamento diário atual (ex.: `+15` para acrescer 15%, `-20` para reduzir 20%). Rejeita campanhas cujo orçamento atual seja zero ou não configurado (`Campaign.CurrentBudgetZero`).
  * **Valor Fixo (`DailyBudget`):** Substitui o orçamento diário pelo novo valor monetário informado (deve ser $\ge 0$).

### 2.2 Regras de Validação Global
* O `WorkspaceId` deve ser um GUID válido e não vazio.
* A lista de operações deve conter entre 1 e 100 itens por lote.
* Todas as campanhas devem pertencer ao `WorkspaceId` contextual. Se uma campanha pertencer a outro workspace, ela é rejeitada individualmente com o código `Campaign.WorkspaceMismatch`.

---

## 3. Contratos de API (OpenAPI)

### Rota: `POST /api/v1/integrations/campaigns/bulk`

#### Cabeçalhos Obrigatórios
* `X-Tenant-Id`: GUID do inquilino contextual.

#### Exemplo de Payload de Entrada (JSON)
```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "operations": [
    {
      "campaignId": "48151623-4200-4000-8000-000000000001",
      "action": 2,
      "reason": "Pausa emergencial de estoque"
    },
    {
      "campaignId": "48151623-4200-4000-8000-000000000002",
      "action": 1,
      "reason": "Reativação de final de semana"
    },
    {
      "campaignId": "48151623-4200-4000-8000-000000000003",
      "action": 3,
      "percentageChange": 15.0,
      "reason": "Escala de orçamento"
    }
  ]
}
```

#### Exemplo de Resposta de Sucesso com Tolerância Parcial (HTTP 200 OK)
```json
{
  "isSuccess": true,
  "isFailure": false,
  "value": {
    "totalRequested": 3,
    "totalSucceeded": 2,
    "totalFailed": 1,
    "succeededItems": [
      {
        "campaignId": "48151623-4200-4000-8000-000000000001",
        "campaignName": "Meta Conversões Brasil",
        "platform": "MetaAds",
        "action": 2,
        "previousStatus": 1,
        "newStatus": 2,
        "previousDailyBudget": 150.00,
        "newDailyBudget": 150.00
      },
      {
        "campaignId": "48151623-4200-4000-8000-000000000002",
        "campaignName": "Google Pesquisa Brand",
        "platform": "GoogleAds",
        "action": 1,
        "previousStatus": 2,
        "newStatus": 1,
        "previousDailyBudget": 200.00,
        "newDailyBudget": 200.00
      }
    ],
    "failedItems": [
      {
        "campaignId": "48151623-4200-4000-8000-000000000003",
        "action": 3,
        "errorCode": "Campaign.CurrentBudgetZero",
        "errorMessage": "Não é possível aplicar ajuste percentual em campanha com orçamento zerado ou não configurado."
      }
    ]
  },
  "error": {
    "code": "",
    "description": "",
    "type": 0
  }
}
```

---

## 4. Frontend Blazor (Tabela Matricial & Modal de Impacto)

* **Componente:** `BulkCampaignEditorMatrix.razor` em `WebApp/Components/Campaigns/`.
* **Página de Rota:** `CampaignsBulkEditPage.razor` em `/workspaces/{workspaceId}/campaigns/bulk`.
* **Fluxo de Usuário:**
  1. Filtro por plataforma (Meta, Google, TikTok, Bing) e status.
  2. Seleção múltipla unitária ou em massa (checkbox no cabeçalho).
  3. Abertura do modal de pré-visualização de impacto com cálculo em tempo real do investimento diário e projeção mensal.
  4. Confirmação do lote e exibição do relatório de resultado com detalhamento de eventuais falhas.
