# Módulo de Automações — Gestão Dinâmica de Budget & Pacing

## 1. Visão Geral

A funcionalidade de **Pacing e Previsão de Fim de Mês** monitora a velocidade de consumo da verba publicitária contratada em relação ao tempo decorrido do ciclo mensal, permitindo que gestores de tráfego identifiquem antecipadamente desvios de subinvestimento ou sobreaquecimento.

---

## 2. Fórmulas Matemáticas & Métricas de Pacing

Dado um ciclo de faturamento com $N_{\text{total}}$ dias, $N_{\text{elapsed}}$ dias decorridos e $N_{\text{remaining}}$ dias restantes:

| Métrica | Fórmula | Descrição |
|---|---|---|
| **Gasto Esperado Linear** | $S_{\text{expected}} = \text{TargetBudget} \times \left(\frac{N_{\text{elapsed}}}{N_{\text{total}}}\right)$ | Montante proporcional ao tempo transcorrido |
| **Ritmo Diário Médio (Run-Rate)** | $\text{DailyRunRate} = \frac{S_{\text{current}}}{N_{\text{elapsed}}}$ | Velocidade média de gasto diário realizada |
| **Índice de Pacing (Ratio)** | $\text{PacingRatio} = \frac{S_{\text{current}}}{S_{\text{expected}}}$ | Razão entre gasto realizado e esperado |
| **Gasto Projetado no Fim do Mês** | $S_{\text{projected}} = \text{DailyRunRate} \times N_{\text{total}}$ | Estimativa final se mantido o ritmo atual |
| **Ritmo Diário Recomendado** | $\text{RequiredRate} = \frac{\max(0, \text{TargetBudget} - S_{\text{current}})}{\max(1, N_{\text{remaining}})}$ | Meta diária para fechar em 100% da verba |

---

## 3. Classificação em 3 Status Operacionais

Com tolerância padrão de $\pm 10\%$ ($\text{tolerance} = 0.10$):
* **No Ritmo (`OnTrack`):** $0.90 \le \text{PacingRatio} \le 1.10$. A velocidade de consumo está em equilíbrio estatístico com a meta contratada.
* **Sobreaquecido (`Over`):** $\text{PacingRatio} > 1.10$. O consumo está acelerado, gerando risco de esgotamento prematuro antes do término do ciclo.
* **Subinvestido (`Under`):** $\text{PacingRatio} < 0.90$. O consumo está abaixo do planejado, gerando risco de sobra de verba orçada.

---

## 4. Endpoints da Web API (`/api/v1/automations/pacing`)

### 4.1 Consulta de Pacing por Workspace
* **Método / Rota:** `GET /api/v1/automations/pacing/workspaces/{workspaceId}`
* **Parâmetros de Consulta:**
  * `year` (int, opcional)
  * `month` (int, opcional, 1 a 12)
  * `asOfDateUtc` (DateTime ISO 8601, opcional)
* **Resposta de Sucesso (HTTP 200 OK):**
```json
{
  "isSuccess": true,
  "value": {
    "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "workspaceName": "E-commerce Estilo",
    "targetBudget": 10000.00,
    "currentSpend": 5000.00,
    "remainingBudget": 5000.00,
    "totalDaysInCycle": 30,
    "elapsedDays": 15,
    "remainingDays": 15,
    "expectedSpendToDate": 5000.00,
    "pacingRatio": 1.0000,
    "pacingPercentage": 100.00,
    "actualDailyRunRate": 333.33,
    "idealDailyRunRate": 333.33,
    "requiredDailyRunRate": 333.33,
    "projectedMonthEndSpend": 10000.00,
    "projectedVariance": 0.00,
    "projectedVariancePercentage": 0.00,
    "status": 1,
    "recommendation": "Ritmo No Ritmo. O consumo está equilibrado com a previsão temporal de fechamento do mês.",
    "currency": "BRL",
    "cycleStartDateUtc": "2026-09-01T00:00:00Z",
    "cycleEndDateUtc": "2026-09-30T23:59:59Z",
    "asOfDateUtc": "2026-09-15T12:00:00Z",
    "campaignBreakdown": [
      {
        "campaignId": "4c9d46e2-5fc8-4720-94d3-1e54881da7fa",
        "campaignName": "Topo de Funil - Meta Ads",
        "platform": "MetaAds",
        "dailyBudget": 200.00,
        "currentSpend": 3000.00,
        "status": 1,
        "pacingRatio": 1.0000
      }
    ]
  },
  "error": {
    "code": null,
    "description": null
  }
}
```

### 4.2 Sumário de Carteira (Portfolio Pacing)
* **Método / Rota:** `GET /api/v1/automations/pacing/portfolio`
* **Parâmetros de Consulta:**
  * `squadId` (Guid, opcional)
  * `year` (int, opcional)
  * `month` (int, opcional)

### 4.3 Simulação Sob Demanda
* **Método / Rota:** `POST /api/v1/automations/pacing/simulate`
* **Payload de Entrada:**
```json
{
  "targetBudget": 20000.00,
  "currentSpend": 12000.00,
  "startDateUtc": "2026-09-01T00:00:00Z",
  "endDateUtc": "2026-09-30T23:59:59Z",
  "asOfDateUtc": "2026-09-15T00:00:00Z",
  "tolerancePercentage": 0.10
}
```

---

## 5. Componente Visual Blazor (`BudgetPacingBar.razor`)

O componente visual reside em `src/Frontend/WebApp/Components/Dashboard/` e renderiza:
1. **Barra de Progresso com Cores Semânticas:**
   - Verde Esmeralda (`#10b981`) para *No Ritmo*;
   - Âmbar / Laranja Queimado (`#f59e0b`) para *Subinvestido*;
   - Vermelho Coral / Carmim (`#ef4444`) para *Sobreaquecido*;
2. **Marcador Temporal Ideal (Needle):** Agulha vertical ciano indicando o ponto de gasto ideal no dia atual;
3. **Barra Fantasma Tracejada:** Demonstrando a projeção esperada para o fim do mês;
4. **Métricas de Apoio e Recomendação:** Exibe o ritmo diário atual e o ritmo necessário restante;
5. **Modo Compacto:** Oculta grid detalhado para visualizações matriciais de carteira.
