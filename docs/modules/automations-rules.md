# Módulo de Automações: Construtor de Regras Cross-Platform (DSL / Condições If-Then)

Este documento descreve a especificação técnica, esquema de dados, contratos de API e comportamentos operacionais do **Construtor de Regras Cross-Platform (DSL)**, correspondente à **Subfase 4.1** da plataforma **AdMetricsPro**.

---

## 1. Visão Geral

O motor de automação permite aos gestores de tráfego configurar regras programadas (*If-Then*) com condições combinadas entre múltiplas redes de anúncios (Meta Ads, Google Ads, TikTok Ads e Bing Ads).

### Destaques Arquiteturais:
- **Motor Desacoplado de Rede:** O serviço `RuleConditionEvaluator` avalia predicados exclusivamente sobre um contexto consolidado em memória (`RuleEvaluationContext`), garantindo performance sub-milisegundo e testes unitários determinísticos.
- **Árvore de Predicados Componível (Composite Pattern):** Suporte a operadores booleanos (`AND`, `OR`, `NOT`) aninhados sem limite de profundidade.
- **Comunicação Inter-Módulos via MediatR:** O módulo `Automations` não acessa repositórios nem DbContext de `Integrations`. O despacho de mutações ocorre via comandos in-memory desacoplados (`PauseCampaignCommand`, `PauseAdCommand`, `AdjustCampaignBudgetCommand`, `ReallocateBudgetCommand`).
- **Persistência Isolada por Tenant:** Regras e estatísticas de execução são gravadas no `TenantDbContext` dedicado.

---

## 2. Especificação da DSL

### 2.1 Operadores Lógicos (`LogicalOperator`)
- `And` (1): Todas as condições do grupo devem ser satisfeitas.
- `Or` (2): Pelo menos uma condição do grupo deve ser satisfeita.
- `Not` (3): Inverte o resultado booleano da condição filha.

### 2.2 Métricas Suportadas (`MetricType`)
| Métrica | Identificador | Descrição |
| :--- | :--- | :--- |
| **CPA** | `Cpa` (1) | Custo por Aquisição / Conversão ($\text{Spend} \div \text{Conversions}$) |
| **ROAS** | `Roas` (2) | Retorno sobre o Investimento em Anúncios ($\text{ConversionValue} \div \text{Spend}$) |
| **CPC** | `Cpc` (3) | Custo Médio por Clique ($\text{Spend} \div \text{Clicks}$) |
| **CPM** | `Cpm` (4) | Custo por Mil Impressões ($(\text{Spend} \div \text{Impressions}) \times 1000$) |
| **CTR** | `Ctr` (5) | Taxa de Cliques em Porcentagem ($(\text{Clicks} \div \text{Impressions}) \times 100$) |
| **Spend** | `Spend` (6) | Investimento total no período |
| **Conversions** | `Conversions` (7) | Volume total de conversões computadas |
| **ConversionValue** | `ConversionValue` (8) | Receita monetária total gerada |

### 2.3 Operadores Relacionais (`ComparisonOperator`)
- `GreaterThan` (1): `>`
- `GreaterThanOrEqual` (2): `>=`
- `LessThan` (3): `<`
- `LessThanOrEqual` (4): `<=`
- `Equal` (5): `==`
- `NotEqual` (6): `!=`

### 2.4 Tipos de Ação (`RuleActionType`)
- `PauseCampaign` (1): Pausa a campanha correspondida ou específica.
- `PauseAd` (2): Pausa o anúncio/criativo correspondido ou específico.
- `AdjustBudgetPercentage` (3): Aplica variação percentual sobre o orçamento diário (ex: `-20.0` para -20%, `+15.0` para +15%).
- `AdjustBudgetFixed` (4): Sobrescreve o orçamento diário com um valor absoluto.
- `ReallocateBudget` (5): Transfere saldo orçamentário entre duas campanhas (origem e destino).

---

## 3. Exemplos Práticos de Esquemas JSON

### Exemplo 1: Gatilho Combinado Cross-Platform (Subfase 4.1.1)
> **Cenário:** *Se CPA do TikTok Ads > R$ 50 nas últimas 48h E Google Ads ROAS > 4.5 &rarr; Reduzir TikTok em 20% e alocar saldo no Google.*

#### Payload de Requisição (`POST /api/v1/automations/rules`):
```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Otimização Cruzada: TikTok CPA Alto -> Realocar no Google Ads",
  "description": "Reduz 20% do budget do TikTok quando o CPA superar R$ 50 nas últimas 48h e transfere para a melhor campanha do Google se o ROAS for superior a 4.5",
  "isEnabled": true,
  "conditionTree": {
    "logicalOperator": 1,
    "conditions": [
      {
        "$type": "metric",
        "platform": "TikTokAds",
        "scope": 2,
        "metric": 1,
        "operator": 1,
        "threshold": 50.00,
        "timeWindowHours": 48,
        "targetEntityId": "a1b2c3d4-e5f6-7890-abcd-111111111111"
      },
      {
        "$type": "metric",
        "platform": "GoogleAds",
        "scope": 2,
        "metric": 2,
        "operator": 1,
        "threshold": 4.50,
        "timeWindowHours": 48,
        "targetEntityId": "b2c3d4e5-f6a7-8901-bcde-222222222222"
      }
    ]
  },
  "actions": [
    {
      "type": 3,
      "platform": "TikTokAds",
      "targetEntityId": "a1b2c3d4-e5f6-7890-abcd-111111111111",
      "value": -20.0
    },
    {
      "type": 5,
      "platform": "CrossPlatform",
      "targetEntityId": "a1b2c3d4-e5f6-7890-abcd-111111111111",
      "destinationEntityId": "b2c3d4e5-f6a7-8901-bcde-222222222222",
      "value": 20.0
    }
  ]
}
```

#### Retorno de Sucesso (`201 Created`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "value": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "error": {
    "code": "None",
    "description": ""
  }
}
```

---

### Exemplo 2: Pausa Automática de Anúncio com Fadiga
> **Cenário:** *Se Anúncio do Meta Ads tiver CTR < 0.5% nas últimas 24h &rarr; Pausar Anúncio imediatamente.*

#### Payload de Requisição:
```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Trava de Fadiga: Pausar Anúncio com CTR Crítico",
  "description": "Pausa anúncios que registrem CTR inferior a 0.5% nas últimas 24h",
  "isEnabled": true,
  "conditionTree": {
    "logicalOperator": 1,
    "conditions": [
      {
        "$type": "metric",
        "platform": "MetaAds",
        "scope": 4,
        "metric": 5,
        "operator": 3,
        "threshold": 0.50,
        "timeWindowHours": 24,
        "targetEntityId": "e1f2a3b4-c5d6-7890-ef01-333333333333"
      }
    ]
  },
  "actions": [
    {
      "type": 2,
      "platform": "MetaAds",
      "targetEntityId": "e1f2a3b4-c5d6-7890-ef01-333333333333"
    }
  ]
}
```

---

## 4. Avaliação e Relatório de Execução (`POST /api/v1/automations/rules/{id}/evaluate`)

Ao disparar a avaliação sob demanda ou via worker programado:

#### Payload de Entrada:
```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

#### Retorno de Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "value": {
    "ruleId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "ruleName": "Otimização Cruzada: TikTok CPA Alto -> Realocar no Google Ads",
    "isTriggered": true,
    "matchedEntitiesCount": 2,
    "actionsDispatchedCount": 2,
    "details": [
      "Gatilho satisfeito: TikTokAds.Cpa[48h] > 50 (Valor real: 60.0000)",
      "Gatilho satisfeito: GoogleAds.Roas[48h] > 4.5 (Valor real: 5.0000)"
    ]
  },
  "error": {
    "code": "None",
    "description": ""
  }
}
```

---

## 5. Catálogo de Endpoints HTTP (Web API)

| Método | Rota | Resumo OpenAPI / Scalar | Códigos HTTP |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/automations/rules` | Cria uma nova regra de automação cross-platform | `201`, `400` |
| `GET` | `/api/v1/automations/rules?workspaceId={guid}` | Lista todas as regras de automação do workspace | `200` |
| `GET` | `/api/v1/automations/rules/{id}?workspaceId={guid}` | Obtém uma regra de automação específica | `200`, `404` |
| `PUT` | `/api/v1/automations/rules/{id}` | Atualiza uma regra de automação existente | `200`, `400`, `404` |
| `PATCH`| `/api/v1/automations/rules/{id}/toggle` | Ativa ou desativa uma regra de automação | `200`, `404` |
| `DELETE`| `/api/v1/automations/rules/{id}?workspaceId={guid}` | Exclui uma regra de automação | `200`, `404` |
| `POST` | `/api/v1/automations/rules/{id}/evaluate` | Avalia e executa uma regra de automação | `200`, `404` |

---

## 6. Tratamento de Casos de Borda e Erros Mapeados

1. **Divisão por Zero em Métricas Derivadas:** Se uma campanha tiver gasto sem conversões ou sem impressões, os KPIs (CPA, CTR, CPC, CPM, ROAS) retornam `0.0000` de forma segura sem lançar exceções.
2. **Dados Ausentes na Janela Solicitada:** Se não houver dados no banco para a janela temporal configurada (ex.: janela de 48h requerida mas banco só possui dados de 72h), a condição folha é considerada não satisfeita com mensagem de diagnóstico explicativa no relatório.
3. **Campanha de Origem sem Orçamento:** Na realocação de saldo (`ReallocateBudget`), se a campanha de origem tiver orçamento zerado ou menor que o valor a transferir, a operação retorna erro semântico tipado (`Reallocation.InsufficientBudget`).
4. **Isolamento Multitenant Rigoroso:** Toda operação valida `WorkspaceId` e resolve `TenantDbContext` dinamicamente a partir do inquilino contextual.
