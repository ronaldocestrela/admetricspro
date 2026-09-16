# Módulo Analytics — Normalização Cambial e Taxonomia Automatizada

Este documento especifica a arquitetura, contratos de dados, regras de negócio cambiais e o motor determinístico de taxonomia automatizada do módulo **Analytics** em .NET 10.

---

## 1. Visão Geral e Objetivos de Negócio

No ecossistema de gestão de tráfego pago multicanal (Meta Ads, Google Ads, TikTok Ads e Bing Ads), as contas de anúncio frequentemente faturam em moedas distintas (ex.: USD para plataformas globais, EUR para clientes multinacionais, BRL para operações nacionais). Além disso, diferentes agências e gestores adotam nomenclaturas variadas para estruturar suas campanhas.

A **Subfase 3.1** estabelece dois pilares fundamentais para dashboards consolidados e inteligência de performance:
1. **Normalização Cambial (`ICurrencyConverter`):** Permite converter qualquer montante de investimento (`Spend`) ou receita (`ConversionValue`) para uma moeda única de exibição (ex.: consolidar tudo em BRL ou USD) aplicando a cotação oficial da data da métrica com cache local resiliente.
2. **Taxonomia Automatizada (`ITaxonomyClassifier`):** Classificador determinístico de alta performance baseado em expressões regulares pré-compiladas que mapeia as convenções de nomes para estágios de funil (`Top`, `Middle`, `Bottom`, `Retention`), tipos de audiência e formatos de mídia.

---

## 2. Normalização Cambial (`ICurrencyConverter`)

### 2.1 Moedas Suportadas (Padrão ISO 4217)
O sistema suporta e valida estritamente os códigos de moeda:
- `BRL` — Real Brasileiro (moeda base padrão)
- `USD` — Dólar Americano
- `EUR` — Euro
- `GBP` — Libra Esterlina

### 2.2 Estratégia de Cotação e Triangulação
As taxas de câmbio são obtidas via `IExchangeRateProvider`:
- Se a moeda de origem for igual à de destino (`source == target`), a taxa é `1.0` com retorno imediato sem consulta externa.
- Para pares heterogêneos, calcula-se a taxa cruzada proporcional:
$$\text{Taxa}(A \to B) = \frac{\text{Cotação}(A \to \text{BRL})}{\text{Cotação}(B \to \text{BRL})}$$
- O arredondamento é padronizado em 6 casas decimais para o fator multiplicador e 4 casas decimais para o montante financeiro final.

### 2.3 Política de Cache Local em Dois Níveis
O serviço `CurrencyConverter` emprega cache em memória (`IMemoryCache`) com chave determinística `fx_{source}_{target}_{yyyyMMdd}`:
1. **Cotações Históricas (`date < hoje`):** Como o câmbio de datas anteriores é imutável, o registro é armazenado com expiração estendida (TTL de 7 dias).
2. **Cotação Intraday (`date == hoje`):** Atualizada com expiração curta (TTL de 30 minutos) para refletir variações de mercado.

---

## 3. Motor de Taxonomia Automatizada (`ITaxonomyClassifier`)

### 3.1 Normalização de Delimitadores de Nomenclatura
Gestores de tráfego utilizam delimitadores diversos em suas campanhas (`_`, `-`, `|`, `/`, `[ ]`, `( )`, `.`). O motor normaliza esses caracteres antes da análise sintática, garantindo correspondência precisa de limites de palavras (`\b`).

### 3.2 Matriz de Classificação de Funil
| Estágio | Padrões Reconhecidos | Exemplo de Campanha |
| :--- | :--- | :--- |
| **Top (Topo / Prospecção)** | `[TOF]`, `TOF`, `Topo`, `Prospeccao`, `Prospecting`, `Awareness`, `Reconhecimento`, `Alcance` | `[TOF] MetaAds_Prospecting_Broad` |
| **Middle (Meio / Consideração)** | `[MOF]`, `MOF`, `Meio`, `Engajamento`, `Engagement`, `Consideracao`, `Consideration`, `Trafego`, `Traffic` | `GoogleAds_Consideration_Category_Traffic` |
| **Bottom (Fundo / Conversão)** | `[BOF]`, `BOF`, `Fundo`, `Remarketing`, `Retargeting`, `RMK`, `Conversao`, `Conversion`, `Vendas`, `Purchase`, `Checkout` | `MetaAds_Remarketing_7D_Catalogo` |
| **Retention (Retenção / LTV)** | `[RET]`, `RET`, `Retencao`, `Reativa[cç][aã]o`, `LTV`, `Churn`, `Winback`, `Fidelizacao` | `Retencao_LTV_Assinantes_VIP` |
| **Unclassified** | Nenhuma convenção conhecida identificada | `Campanha 12345 2026-09` |

### 3.3 Tipologia de Audiência e Formato de Criativo
- **Tipo de Audiência (`AudienceType`):** `Broad` (Aberto/Amplo), `Lookalike` (LAL/Semelhante), `Interest` (Interesses), `CustomAudience` (Lista/Visitantes), `SearchBrand` (Institucional/Marca), `SearchNonBrand` (Genérico).
- **Formato de Criativo (`CreativeType`):** `Video` (Reels, Shorts, TikTok, YouTube), `Carousel` (Carrossel, DPA), `Image` (Estática, Banner, Feed), `SearchText` (Rede de Pesquisa), `Dynamic` (PMax, Advantage+).

---

## 4. Endpoints RESTful Web API

### 4.1 Conversão Monetária Pontual
- **Rota:** `GET /api/v1/analytics/currency/convert?amount=100&sourceCurrency=USD&targetCurrency=BRL&date=2026-09-15`
- **Sumário OpenAPI:** `Converte montante monetário pela cotação do dia`
- **Exemplo de Retorno Sucesso (HTTP 200):**
```json
{
  "isSuccess": true,
  "value": {
    "originalAmount": 100.0,
    "convertedAmount": 545.0,
    "sourceCurrency": "USD",
    "targetCurrency": "BRL",
    "exchangeRate": 5.45,
    "date": "2026-09-15T00:00:00Z",
    "effectiveDateUtc": "2026-09-15T21:20:00Z"
  }
}
```

### 4.2 Conversão Monetária em Lote
- **Rota:** `POST /api/v1/analytics/currency/convert-batch`
- **Sumário OpenAPI:** `Converte em lote múltiplos valores monetários para moeda unificada`
- **Exemplo de Payload (JSON):**
```json
{
  "targetCurrency": "BRL",
  "items": [
    { "amount": 100.0, "sourceCurrency": "USD", "date": "2026-09-14T00:00:00Z" },
    { "amount": 50.0, "sourceCurrency": "EUR", "date": "2026-09-15T00:00:00Z" }
  ]
}
```
- **Exemplo de Retorno (HTTP 200):**
```json
{
  "isSuccess": true,
  "value": [
    {
      "originalAmount": 100.0,
      "convertedAmount": 545.0,
      "sourceCurrency": "USD",
      "targetCurrency": "BRL",
      "exchangeRate": 5.45,
      "date": "2026-09-14T00:00:00Z",
      "effectiveDateUtc": "2026-09-15T21:20:00Z"
    },
    {
      "originalAmount": 50.0,
      "convertedAmount": 297.5,
      "sourceCurrency": "EUR",
      "targetCurrency": "BRL",
      "exchangeRate": 5.95,
      "date": "2026-09-15T00:00:00Z",
      "effectiveDateUtc": "2026-09-15T21:20:00Z"
    }
  ]
}
```

### 4.3 Classificação de Taxonomia Individual
- **Rota:** `POST /api/v1/analytics/taxonomy/classify`
- **Sumário OpenAPI:** `Classifica automaticamente estágio de funil, público e formato`
- **Exemplo de Payload (JSON):**
```json
{
  "campaignName": "[TOF] Prospecção LAL 1% Compradores",
  "adSetName": "Conjunto Aberto",
  "adName": "Criativo Vídeo Review 01",
  "referenceId": "cmp_987654"
}
```
- **Exemplo de Retorno (HTTP 200):**
```json
{
  "isSuccess": true,
  "value": {
    "referenceId": "cmp_987654",
    "campaignName": "[TOF] Prospecção LAL 1% Compradores",
    "adSetName": "Conjunto Aberto",
    "adName": "Criativo Vídeo Review 01",
    "funnelStage": "Top",
    "audienceType": "Lookalike",
    "creativeType": "Video",
    "tags": [
      "Top",
      "Lookalike",
      "Video"
    ]
  }
}
```

### 4.4 Classificação de Taxonomia em Lote
- **Rota:** `POST /api/v1/analytics/taxonomy/classify-batch`
- **Sumário OpenAPI:** `Classifica em lote taxonomias de múltiplas campanhas e anúncios`
- **Exemplo de Payload (JSON):**
```json
{
  "items": [
    {
      "campaignName": "[TOF] Prospecção Aberta - Vídeo",
      "adSetName": "Conjunto 01",
      "adName": "Vídeo 01",
      "referenceId": "ref-1"
    },
    {
      "campaignName": "[BOF] Remarketing Checkout - Carrossel",
      "adSetName": "Conjunto RMK",
      "adName": "Carrossel 01",
      "referenceId": "ref-2"
    }
  ]
}
```
