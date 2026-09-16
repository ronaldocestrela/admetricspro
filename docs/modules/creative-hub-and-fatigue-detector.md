# Especificação Funcional & Técnica: Creative Hub & Detector de Fadiga de Criativos

## 1. Visão Geral
O **Creative Hub & Detector de Fadiga de Criativos** é o subsistema de inteligência analítica do AdMetricsPro responsável por:
1. **Diagnóstico Preditivo de Fadiga:** Avaliação contínua da performance de peças publicitárias (imagens e vídeos) ao longo dos últimos 7 dias, identificando o fenômeno de saturação de público onde a taxa de cliques (CTR) sofre declínio contínuo acompanhado por frequência média/acumulada elevada.
2. **Agregação Cross-Platform (Meta Ads vs. TikTok Ads):** Agrupamento de métricas por ativo de mídia (`MediaFingerprint`), permitindo contrastar a eficiência do mesmo vídeo ou imagem quando veiculado simultaneamente em redes distintas.
3. **Avisos Visuais & Recomendações Acionáveis:** Emissão de alertas em destaque na interface do gestor de tráfego, com justificativa analítica em linguagem natural e botão de substituição direta do criativo.

---

## 2. Modelagem Matemática & Regras de Negócio

### 2.1 Detecção Algorítmica de Fadiga (`AdFatigueDetector`)
O motor analisa uma série temporal diária de até 7 dias para cada anúncio ativo:
- **Janela Mínima de Dados:** É necessário um histórico mínimo de **3 dias** com impressões $> 0$. Caso contrário, o sistema retorna `CreativeFatigue.InsufficientData`.
- **Taxa de Variação de CTR ($\Delta CTR\%$):**
  $$\Delta CTR\% = \frac{CTR_{atual} - CTR_{inicial}}{CTR_{inicial}} \times 100$$
- **Inclinação da Reta de Tendência ($Slope$):**
  Calculada via Regressão Linear Simples dos valores diários de CTR ao longo dos dias analisados ($x = 0, \dots, n-1$):
  $$Slope = \frac{n \sum (x y) - (\sum x)(\sum y)}{n \sum (x^2) - (\sum x)^2}$$
- **Classificação de Saúde do Criativo:**
  1. **Fadiga Crítica (`Fatigued`):**
     - Condição: $\Delta CTR\% \le -20.0\%$ **E** $Slope < -0.01$ **E** Frequência Média $\ge 2.0x$.
     - Indicador `ReplacementSuggested`: `true`.
     - Ação recomendada: *"Substitua a peça publicitária por uma nova variação ou renove os ganchos visuais e textos imediatamente para estancar a degradação de CPA."*
  2. **Alerta de Desgaste (`Warning`):**
     - Condição: $\Delta CTR\% \le -10.0\%$ **OU** Frequência Média $\ge 2.0x$ **OU** $Slope < -0.01$.
     - Indicador `ReplacementSuggested`: `false`.
     - Ação recomendada: *"Monitore as métricas do criativo nos próximos dias e prepare variações na esteira de produção para substituição preventiva."*
  3. **Saudável (`Healthy`):**
     - Condição: Nenhuma das condições anteriores atendida.
     - Indicador `ReplacementSuggested`: `false`.
     - Ação recomendada: *"Mantenha a veiculação regular do criativo. Avalie escala de orçamento caso os indicadores de conversão permaneçam positivos."*

### 2.2 Agregação por Ativo de Mídia & Comparação Cross-Platform (`CreativeHubAggregator`)
- Consolida métricas de performance (Spend, Impressões, Cliques, Conversões, Receita, CTR, CPA e ROAS) de cada canal.
- **Eleição da Rede Vencedora (`WinningPlatform`):**
  - Se ambas as redes possuírem conversões: vence o canal com **menor CPA**. Em caso de empate de CPA, desempata pelo maior ROAS.
  - Se apenas uma rede converteu: vence o canal com conversões registradas.
  - Se nenhuma converteu: vence o canal com **maior CTR**.

---

## 3. Contratos de API (OpenAPI & Scalar)

### 3.1 Visão Geral do Workspace: `GET /api/v1/analytics/creatives/overview`
Recupera os contadores estatísticos e a lista de criativos monitorados no workspace.

#### Parâmetros de Consulta (Query Parameters)
* `workspaceId` (GUID, obrigatório): Identificador do workspace do inquilino.
* `startDate` (DateTime ISO, opcional): Início da janela.
* `endDate` (DateTime ISO, opcional): Fim da janela.

#### Exemplo de Resposta (HTTP 200 OK)
```json
{
  "isSuccess": true,
  "value": {
    "workspaceId": "48151623-4200-4000-8000-000000000001",
    "totalCreatives": 12,
    "fatiguedCreativesCount": 2,
    "warningCreativesCount": 3,
    "healthyCreativesCount": 7,
    "replacementsSuggestedCount": 2,
    "creatives": [
      {
        "adId": "11111111-2222-3333-4444-555555555555",
        "adName": "Vídeo Teaser Coleção Inverno",
        "platform": "MetaAds",
        "previewUrl": "https://cdn.example.com/teaser.mp4",
        "status": "Fatigued",
        "analyzedDays": 7,
        "initialCtr": 3.20,
        "currentCtr": 1.40,
        "ctrChangePercentage": -56.25,
        "ctrTrendSlope": -0.3000,
        "averageFrequency": 3.20,
        "currentFrequency": 3.40,
        "replacementSuggested": true,
        "reason": "Fadiga crítica detectada: queda de 56.3% no CTR nos últimos 7 dias acompanhada de frequência média elevada (3.2x). O público atingido está saturado.",
        "actionRecommendation": "Substitua a peça publicitária por uma nova variação ou renove os ganchos visuais e textos imediatamente para estancar a degradação de CPA."
      }
    ]
  },
  "error": {
    "code": "None",
    "description": "None"
  }
}
```

---

### 3.2 Diagnóstico Pontual de Fadiga: `GET /api/v1/analytics/creatives/fatigue`
Analisa a série temporal dos últimos 7 dias de um criativo específico.

#### Parâmetros de Consulta
* `workspaceId` (GUID, obrigatório)
* `adId` (GUID, obrigatório)
* `startDate` (DateTime ISO, opcional)
* `endDate` (DateTime ISO, opcional)

---

### 3.3 Comparativo Cross-Platform: `GET /api/v1/analytics/creatives/comparison`
Contrasta a eficiência da mesma peça criativa veiculada no Meta Ads vs. TikTok Ads.

#### Parâmetros de Consulta
* `workspaceId` (GUID, obrigatório)
* `assetFingerprint` (string, obrigatório): Hash ou identificador da mídia.
* `assetName` (string, opcional)
* `previewUrl` (string, opcional)

#### Exemplo de Resposta (HTTP 200 OK)
```json
{
  "isSuccess": true,
  "value": {
    "assetFingerprint": "hash_video_promo_2026",
    "assetName": "Vídeo Teaser Coleção Inverno",
    "previewUrl": "https://cdn.example.com/teaser.mp4",
    "metaMetrics": {
      "platform": "MetaAds",
      "adCount": 1,
      "spend": 1000.00,
      "impressions": 50000,
      "clicks": 1500,
      "conversions": 50.00,
      "conversionValue": 5000.00,
      "ctr": 3.00,
      "cpc": 0.67,
      "cpa": 20.00,
      "roas": 5.00
    },
    "tikTokMetrics": {
      "platform": "TikTokAds",
      "adCount": 1,
      "spend": 1000.00,
      "impressions": 60000,
      "clicks": 1200,
      "conversions": 25.00,
      "conversionValue": 2500.00,
      "ctr": 2.00,
      "cpc": 0.83,
      "cpa": 40.00,
      "roas": 2.50
    },
    "winningPlatform": "MetaAds",
    "cpaDifferencePercentage": -50.00,
    "ctrDifferencePercentage": 50.00,
    "efficiencySummary": "MetaAds entregou melhor custo por conversão (CPA de R$ 20.00 vs. R$ 40.00 no TikTokAds). Sugerida concentração de orçamento no MetaAds para este criativo."
  },
  "error": {
    "code": "None",
    "description": "None"
  }
}
```

---

## 4. Arquitetura Frontend em Blazor Server

O frontend respeita a regra fundamental de **Zero Acesso Direto ao Banco**:
- Consumo estritamente via `ICreativeHubClientService` / `CreativeHubClientService` com headers multitenant `X-Tenant-Id`.
- Componentes atômicos:
  - `CreativeFatigueBadge.razor`: Badge semântico (Saudável, Alerta de Desgaste, Fadiga Crítica + Troca Sugerida).
  - `CreativeFatigueAlertBanner.razor`: Aviso visual de alta prioridade na tela do gestor com recomendação de substituição.
  - `CreativeComparisonCard.razor`: Grid comparativo lado a lado de Meta Ads vs. TikTok Ads destacando métricas e plataforma vencedora.
  - `CreativeHubPage.razor`: Painel completo de monitoramento de criativos.
