# Especificação Funcional & Técnica: Copiloto de Otimização via IA (Auditor de Tráfego)

## 1. Visão Geral
O **Copiloto de Otimização via IA (Auditor de Tráfego)** é o assistente inteligente autônomo do AdMetricsPro responsável por:
1. **Detecção Algorítmica de Anomalias de Leilão:**
   - **Sobreposição de Públicos no Meta Ads (*Audience Overlap*):** Identificação de conjuntos de anúncios (`AdSet`) ativos competindo entre si pelas mesmas fatias de audiência (interesses, lookalikes ou critérios demográficos sobrepostos em $\ge 30\%$), provocando auto-concorrência interna e inflação de CPM.
   - **Disputa e Canibalização de Termos de Busca (Google Ads vs. Bing Ads):** Identificação de palavras-chave idênticas ou concorrentes disputadas simultaneamente entre campanhas do Google Ads e Bing Ads com alta disparidade de custo por aquisição (CPA) ou custo por clique (CPC).
2. **Gerador de Diagnóstico Diário Sintetizado em Texto Natural:**
   - Orquestração das análises estatísticas em um briefing executivo consolidado com três seções: **Vitórias do Período (*Wins*)**, **Riscos Operacionais (*Risks*)** e **Anomalias Críticas**, acompanhado de estimativa monetária da economia mensal projetada.
3. **Mecanismo de Execução em 1 Clique (*1-Click Remediation*):**
   - Cada recomendação diagnóstica disponibiliza uma ação imediata (pausar conjunto redundante, negativar palavra-chave em canal de alto CPA, aplicar exclusão mútua de audiência ou limitar orçamento), despachada via API com registro na trilha imutável de auditoria do Tenant.

---

## 2. Modelagem Matemática & Algoritmos de Detecção

### 2.1 Detecção de Sobreposição de Públicos no Meta Ads (`MetaAudienceOverlapDetector`)
- **Conjuntos Analisados:** Conjuntos com status `Active` e histórico de veiculação recente.
- **Índice de Similaridade de Jaccard ($J$):**
  $$J(A, B) = \frac{|Tags_A \cap Tags_B|}{|Tags_A \cup Tags_B|}$$
- **Taxa de Sobreposição Percentual:**
  $$Overlap\% = J(A, B) \times 100$$
- **Classificação de Severidade:**
  - **Crítica (`Critical`):** $Overlap\% \ge 50.0\%$
  - **Alta (`High`):** $30.0\% \le Overlap\% < 50.0\%$
  - **Saudável / Sem Alerta:** $Overlap\% < 30.0\%$
- **Regra de Remediação:** Eleição do conjunto com pior eficiência histórica (maior CPA ou maior CPM) para sugestão de pausa imediata (`PauseAdSet`), preservando o conjunto mais rentável.

### 2.2 Detecção de Canibalização de Termos de Busca (`SearchTermCannibalizationDetector`)
- **Normalização de Termos:** Extração do radical da palavra-chave com remoção de acentos e padronização em minúsculas (ex.: *"Gestão de Tráfego"* e *"gestao de trafego"*).
- **Razão de Disparidade de CPA ($Disparity$):**
  $$Disparity = \frac{CPA_{Canal Ineficiente}}{CPA_{Canal Eficiente}}$$
- **Limiares de Severidade:**
  - **Crítica (`Critical`):** $Disparity \ge 2.0\times$ (ou CPA mais que o dobro no canal concorrente com conversões registradas).
  - **Alta (`High`):** $1.75\times \le Disparity < 2.0\times$.
- **Cálculo de Desperdício Mensal Estimado:**
  $$Desperdício = (CPA_{Canal Ineficiente} - CPA_{Canal Eficiente}) \times Conversões_{Canal Ineficiente}$$
- **Regra de Remediação:** Sugestão de negativação da palavra-chave no canal de pior CPA (`AddNegativeKeyword`) para concentrar o investimento no canal de melhor retorno.

### 2.3 Síntese Diária em Linguagem Natural (`TrafficAuditorSynthesizer`)
- Sintetiza um relatório estruturado diário consolidando:
  - Resumo Executivo com quantidade de anomalias críticas e de alta prioridade.
  - Vitórias e conquistas de campanhas operando com retorno positivo.
  - Riscos detalhados de saturação, canibalização e leilões inflacionados.
  - Soma total da economia financeira potencial estimada.

---

## 3. Contratos de API (OpenAPI & Scalar)

### 3.1 Consulta de Diagnóstico Diário: `GET /api/v1/analytics/copilot/diagnostic`
Recupera o diagnóstico diário sintetizado e a lista completa de anomalias encontradas para o workspace.

#### Parâmetros de Consulta (Query Parameters)
* `workspaceId` (GUID, obrigatório): Identificador do workspace.
* `date` (DateTime ISO, opcional): Data de referência da análise (default: data atual UTC).

#### Exemplo de Resposta (HTTP 200 OK)
```json
{
  "isSuccess": true,
  "value": {
    "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "reportDate": "2026-09-16T00:00:00Z",
    "executiveSummary": "Diagnóstico do Copiloto: Foram identificadas 2 anomalias críticas e 1 alerta de alta prioridade demandando ação imediata. A aplicação das recomendações sugeridas pode gerar uma economia potencial de R$ 1.850,00 por mês.",
    "winsSummary": "Campanhas de topo e criativos da esteira recente continuam entregando conversões consistentes dentro do CPA meta.",
    "risksSummary": "Sobreposição de públicos detectada no Meta Ads (1 ocorrências), provocando inflação de CPM e canibalização de leilão interno. Canibalização e disputa de termos de busca (1 ocorrências) entre Google e Bing, gerando dispersão de verba em canais de alto CPA.",
    "audienceOverlapAnomalies": [
      {
        "adSetIdA": "11111111-1111-1111-1111-111111111111",
        "adSetNameA": "Conjunto 1 - Empreendedorismo",
        "campaignIdA": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "campaignNameA": "Conversão Leads Q3",
        "adSetIdB": "22222222-2222-2222-2222-222222222222",
        "adSetNameB": "Conjunto 2 - Startups e Gestão",
        "campaignIdB": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "campaignNameB": "Conversão Leads Q3",
        "overlapPercentage": 55.0,
        "cpmA": 28.50,
        "cpmB": 42.00,
        "severity": "Critical",
        "description": "Sobreposição crítica de público detectada (55.0% de similaridade). Os conjuntos disputam o mesmo público no Meta Ads gerando auto-concorrência.",
        "suggestedAction": {
          "actionId": "77777777-7777-7777-7777-777777777777",
          "actionType": "PauseAdSet",
          "targetEntityId": "22222222-2222-2222-2222-222222222222",
          "targetEntityName": "Conjunto 2 - Startups e Gestão",
          "platform": "MetaAds",
          "title": "Pausar conjunto redundante (Conjunto 2 - Startups e Gestão)",
          "description": "Pausa o conjunto de pior CPA (R$ 110,00) para estancar a auto-concorrência contra Conjunto 1 (CPA R$ 75,00).",
          "parameters": {
            "adSetIdToPause": "22222222-2222-2222-2222-222222222222"
          },
          "isApplied": false,
          "appliedAtUtc": null
        },
        "sharedTargetingTags": ["empreendedorismo", "startups", "marketing digital"]
      }
    ],
    "searchCannibalizationAnomalies": [
      {
        "searchTerm": "gestao de trafego pago",
        "channelA": "GoogleAds",
        "campaignIdA": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        "campaignNameA": "Search - Leads Alta Intenção",
        "cpcA": 6.00,
        "cpaA": 150.00,
        "spendA": 1500.00,
        "channelB": "BingAds",
        "campaignIdB": "cccccccc-cccc-cccc-cccc-cccccccccccc",
        "campaignNameB": "Microsoft Search - Conversão",
        "cpcB": 2.50,
        "cpaB": 50.00,
        "spendB": 400.00,
        "disparityRatio": 3.0,
        "estimatedMonthlyWastedSpend": 1000.00,
        "severity": "Critical",
        "description": "Canibalização severa detectada no termo 'gestao de trafego pago'. O canal GoogleAds está 3.0x mais caro que o BingAds.",
        "suggestedAction": {
          "actionId": "88888888-8888-8888-8888-888888888888",
          "actionType": "AddNegativeKeyword",
          "targetEntityId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          "targetEntityName": "Search - Leads Alta Intenção",
          "platform": "GoogleAds",
          "title": "Negativar palavra-chave em GoogleAds (gestao de trafego pago)",
          "description": "Adiciona o termo como palavra-chave negativa na campanha de alto CPA.",
          "parameters": {
            "keyword": "gestao de trafego pago",
            "platformToNegate": "GoogleAds"
          },
          "isApplied": false,
          "appliedAtUtc": null
        }
      }
    ],
    "actions": [
      {
        "actionId": "77777777-7777-7777-7777-777777777777",
        "actionType": "PauseAdSet",
        "targetEntityId": "22222222-2222-2222-2222-222222222222",
        "targetEntityName": "Conjunto 2 - Startups e Gestão",
        "platform": "MetaAds",
        "title": "Pausar conjunto redundante (Conjunto 2 - Startups e Gestão)",
        "description": "Pausa o conjunto de pior CPA para estancar auto-concorrência.",
        "parameters": {},
        "isApplied": false,
        "appliedAtUtc": null
      }
    ],
    "estimatedMonthlySavings": 1850.00,
    "criticalAnomaliesCount": 2,
    "highAnomaliesCount": 0
  },
  "error": {
    "code": "None",
    "description": "None"
  }
}
```

---

### 3.2 Execução de Ação em 1 Clique: `POST /api/v1/analytics/copilot/actions/execute`
Executa imediatamente a ação corretiva selecionada pelo gestor.

#### Payload de Entrada (JSON)
```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "actionId": "77777777-7777-7777-7777-777777777777",
  "actionType": "PauseAdSet",
  "targetEntityId": "22222222-2222-2222-2222-222222222222",
  "targetEntityName": "Conjunto 2 - Startups e Gestão",
  "platform": "MetaAds",
  "title": "Pausar conjunto redundante (Conjunto 2 - Startups e Gestão)",
  "description": "Pausa o conjunto de pior CPA para estancar auto-concorrência.",
  "parameters": {
    "adSetIdToPause": "22222222-2222-2222-2222-222222222222"
  }
}
```

#### Exemplo de Resposta (HTTP 200 OK)
```json
{
  "isSuccess": true,
  "value": {
    "actionId": "77777777-7777-7777-7777-777777777777",
    "success": true,
    "message": "Ação 'Pausar conjunto redundante (Conjunto 2 - Startups e Gestão)' executada com sucesso em 1 clique.",
    "executedAtUtc": "2026-09-16T15:00:00Z"
  },
  "error": {
    "code": "None",
    "description": "None"
  }
}
```
