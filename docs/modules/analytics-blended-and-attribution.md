# Especificação do Módulo: Métricas Blended, MER e Atribuição Multicanal

## 1. Visão Geral e Propósito

O módulo **Analytics** consolida o desempenho financeiro e analítico do **AdMetricsPro**, permitindo que gestores de tráfego, agências e executivos compreendam a eficiência consolidada do investimento em mídia paga de múltiplos canais (Meta Ads, Google Ads, TikTok Ads, Bing Ads).

Nesta subfase, o motor analítico resolve dois desafios cruciais de inteligência de marketing:
1. **Métricas Agregadas / Blended e MER:** Elimina a miopia de olhar cada rede de anúncios de forma isolada, calculando o **MER (*Marketing Efficiency Ratio*)**, **Blended ROAS**, **Blended CAC** e métricas operacionais agregadas (CPA, CPC, CPM, CTR) com conversão cambial multi-moeda integrada.
2. **Atribuição Multicanal (*Multi-Touch Attribution*):** Mapeia como os pontos de contato distribuídos ao longo do tempo influenciam a conversão final, fornecendo comparações determinísticas entre os modelos de **Primeiro Clique (*First-Touch*)**, **Último Clique (*Last-Touch*)** e **Linear ($1/N$)**, além de contabilizar o volume de **Conversões Assistidas (*Assisted Conversions*)**.

---

## 2. Fórmulas Matemáticas e Métricas Consolidadas

### 2.1 Marketing Efficiency Ratio (MER)
Mede a eficiência global do investimento publicitário em relação à receita:
$$\text{MER} = \frac{\text{Receita Total (Loja/E-commerce ou Conversões Consolidadas)}}{\text{Gasto Total Consolidado}}$$
*Se a receita externa da loja não for informada, o sistema adota a soma das receitas de conversão das plataformas como fallback.*

### 2.2 Blended ROAS (Return on Ad Spend Agregado)
$$\text{Blended ROAS} = \frac{\sum \text{Receita de Conversões de Todas as Redes}}{\sum \text{Investimento Total Consolidado}}$$

### 2.3 Blended CAC (Custo de Aquisição de Clientes Agregado)
$$\text{Blended CAC} = \frac{\sum \text{Investimento Total Consolidado}}{\sum \text{Novos Clientes Adquiridos}}$$

### 2.4 Métricas Operacionais Blended
- **Blended CPA:** $\text{Gasto Total} \div \text{Total de Conversões}$
- **Blended CPC:** $\text{Gasto Total} \div \text{Total de Cliques}$
- **Blended CPM:** $(\text{Gasto Total} \div \text{Total de Impressões}) \times 1000$
- **Blended CTR (%):** $(\text{Total de Cliques} \div \text{Total de Impressões}) \times 100$
- **Participação de Investimento (*Spend Share %*):** $(\text{Gasto da Plataforma} \div \text{Gasto Total Consolidado}) \times 100$

*Tratamento de Exceções:* Quando qualquer denominador for zero, o sistema retorna `0m` com segurança, prevenindo qualquer falha de divisão por zero.

---

## 3. Modelos de Atribuição Multicanal

| Modelo | Regra de Crédito | Melhor Utilização |
|---|---|---|
| **Primeiro Clique (*First-Touch*)** | 100% da conversão e da receita são atribuídos ao primeiro ponto de contato da jornada. | Avaliar canais geradores de demanda e prospecção de topo de funil. |
| **Último Clique (*Last-Touch*)** | 100% da conversão e da receita são atribuídos ao último canal anterior à conversão. | Avaliar canais de fechamento e conversão direta de fundo de funil. |
| **Linear** | A conversão e a receita são distribuídas uniformemente ($1/N$) entre todos os pontos de contato da jornada. | Visão balanceada do esforço integrado de mídia ao longo do funil. |
| **Conversões Assistidas (*Assisted Conversions*)** | Contabiliza quantas jornadas aquele canal influenciou como ponto intermediário sem ter sido o último toque. | Identificar canais de consideração e tráfego assistido que seriam subestimados no último clique. |

*Filtro Cronológico:* Touchpoints com data superior ao momento da conversão são automaticamente descartados da análise.

---

## 4. Contratos de API e Exemplos JSON

### 4.1 Cálculo de Métricas Blended e MER

**Endpoint:** `POST /api/v1/analytics/blended-metrics`

#### Payload de Requisição (`CalculateBlendedMetricsApiRequest`)
```json
{
  "targetCurrency": "BRL",
  "totalStoreRevenue": 65000.00,
  "totalNewCustomers": 120,
  "items": [
    {
      "platform": "Meta",
      "date": "2026-09-15T00:00:00Z",
      "spend": 1000.00,
      "currency": "USD",
      "impressions": 60000,
      "clicks": 1500,
      "conversions": 80,
      "conversionValue": 3500.00,
      "newCustomers": 70
    },
    {
      "platform": "Google",
      "date": "2026-09-15T00:00:00Z",
      "spend": 4500.00,
      "currency": "BRL",
      "impressions": 40000,
      "clicks": 1200,
      "conversions": 50,
      "conversionValue": 14000.00,
      "newCustomers": 50
    }
  ]
}
```

#### Payload de Retorno com Sucesso (`Result<BlendedMetricsDto>`)
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": null,
    "message": null
  },
  "value": {
    "targetCurrency": "BRL",
    "totalSpend": 10000.00,
    "totalConversionValue": 33250.00,
    "totalStoreRevenue": 65000.00,
    "totalImpressions": 100000,
    "totalClicks": 2700,
    "totalConversions": 130.0,
    "totalNewCustomers": 120,
    "marketingEfficiencyRatio": 6.50,
    "blendedRoas": 3.325,
    "blendedCac": 83.33,
    "blendedCpa": 76.92,
    "blendedCpc": 3.70,
    "blendedCpm": 100.00,
    "blendedCtr": 2.70,
    "channelBreakdowns": [
      {
        "platform": "Meta",
        "spend": 5500.00,
        "spendSharePercentage": 55.00,
        "impressions": 60000,
        "clicks": 1500,
        "conversions": 80.0,
        "conversionValue": 19250.00,
        "roas": 3.50,
        "cpa": 68.75,
        "cpc": 3.67,
        "cpm": 91.67,
        "ctr": 2.50
      },
      {
        "platform": "Google",
        "spend": 4500.00,
        "spendSharePercentage": 45.00,
        "impressions": 40000,
        "clicks": 1200,
        "conversions": 50.0,
        "conversionValue": 14000.00,
        "roas": 3.11,
        "cpa": 90.00,
        "cpc": 3.75,
        "cpm": 112.50,
        "ctr": 3.00
      }
    ]
  }
}
```

---

### 4.2 Análise e Comparação de Atribuição Multicanal

**Endpoint:** `POST /api/v1/analytics/attribution`

#### Payload de Requisição (`CalculateAttributionApiRequest`)
```json
{
  "channelCosts": {
    "TikTok": 300.00,
    "Meta": 400.00,
    "Google": 500.00
  },
  "journeys": [
    {
      "journeyId": "order_98712",
      "customerId": "cust_452",
      "convertedAtUtc": "2026-09-15T20:30:00Z",
      "conversionValue": 1200.00,
      "touchpoints": [
        {
          "channel": "TikTok",
          "campaignName": "video_viral_tof",
          "occurredAtUtc": "2026-09-10T14:00:00Z",
          "touchType": 1,
          "cost": 1.50
        },
        {
          "channel": "Meta",
          "campaignName": "rmk_catalog_mof",
          "occurredAtUtc": "2026-09-13T19:00:00Z",
          "touchType": 1,
          "cost": 2.20
        },
        {
          "channel": "Google",
          "campaignName": "search_brand_bof",
          "occurredAtUtc": "2026-09-15T20:15:00Z",
          "touchType": 1,
          "cost": 3.10
        }
      ]
    }
  ]
}
```

#### Payload de Retorno com Sucesso (`Result<AttributionComparisonDto>`)
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": null,
    "message": null
  },
  "value": {
    "totalJourneys": 1,
    "totalConversions": 1.0,
    "totalConversionValue": 1200.00,
    "firstTouchChannels": [
      {
        "channel": "TikTok",
        "attributedConversions": 1.0,
        "attributedRevenue": 1200.00,
        "totalTouchpoints": 1,
        "firstTouchCount": 1,
        "lastTouchCount": 0,
        "assistedConversionsCount": 1,
        "attributedRoas": 4.00,
        "attributedCpa": 300.00
      },
      {
        "channel": "Meta",
        "attributedConversions": 0.0,
        "attributedRevenue": 0.00,
        "totalTouchpoints": 1,
        "firstTouchCount": 0,
        "lastTouchCount": 0,
        "assistedConversionsCount": 1,
        "attributedRoas": 0.00,
        "attributedCpa": null
      },
      {
        "channel": "Google",
        "attributedConversions": 0.0,
        "attributedRevenue": 0.00,
        "totalTouchpoints": 1,
        "firstTouchCount": 0,
        "lastTouchCount": 1,
        "assistedConversionsCount": 0,
        "attributedRoas": 0.00,
        "attributedCpa": null
      }
    ],
    "lastTouchChannels": [
      {
        "channel": "Google",
        "attributedConversions": 1.0,
        "attributedRevenue": 1200.00,
        "totalTouchpoints": 1,
        "firstTouchCount": 0,
        "lastTouchCount": 1,
        "assistedConversionsCount": 0,
        "attributedRoas": 2.40,
        "attributedCpa": 500.00
      }
    ],
    "linearChannels": [
      {
        "channel": "TikTok",
        "attributedConversions": 0.3333,
        "attributedRevenue": 400.00,
        "totalTouchpoints": 1,
        "firstTouchCount": 1,
        "lastTouchCount": 0,
        "assistedConversionsCount": 1,
        "attributedRoas": 1.33,
        "attributedCpa": 900.09
      },
      {
        "channel": "Meta",
        "attributedConversions": 0.3333,
        "attributedRevenue": 400.00,
        "totalTouchpoints": 1,
        "firstTouchCount": 0,
        "lastTouchCount": 0,
        "assistedConversionsCount": 1,
        "attributedRoas": 1.00,
        "attributedCpa": 1200.12
      },
      {
        "channel": "Google",
        "attributedConversions": 0.3333,
        "attributedRevenue": 400.00,
        "totalTouchpoints": 1,
        "firstTouchCount": 0,
        "lastTouchCount": 1,
        "assistedConversionsCount": 0,
        "attributedRoas": 0.80,
        "attributedCpa": 1500.15
      }
    ]
  }
}
```

---

## 5. Códigos de Erro Mapeados

| Código | Mensagem | Causa |
|---|---|---|
| `BlendedMetrics.InvalidCurrency` | A moeda de destino deve ser um código ISO válido de 3 caracteres. | Moeda alvo nula, vazia ou inválida. |
| `BlendedMetrics.InvalidAmount` | Os valores de métricas não podem ser negativos. | Spend ou ConversionValue menor que zero. |
| `BlendedMetrics.InvalidNewCustomers` | O número total de novos clientes não pode ser negativo. | Parâmetro de novos clientes negativo. |
| `Attribution.InvalidConversionValue` | O valor de conversão da jornada não pode ser negativo. | Jornada com receita negativa. |
| `Attribution.InvalidTouchpointCost` | O custo do ponto de contato não pode ser negativo. | Ponto de contato com custo negativo. |
