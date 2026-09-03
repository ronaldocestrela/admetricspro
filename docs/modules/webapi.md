# Módulo WebApi: Host ASP.NET Core 10, OpenAPI & Scalar UI

## 1. Visão Geral
O projeto `src/Backend/WebApi` atua como o ponto de entrada (Host) HTTP para o ecossistema backend do **AdMetricsPro**. Ele é responsável por:
- Inicializar o runtime do ASP.NET Core 10 com roteamento centralizado (`LowercaseUrls = true`).
- Configurar os pipelines de middleware HTTP (roteamento, autenticação, autorização e tratamento de requisições).
- Expor a documentação viva e interativa de contratos via **OpenAPI v1** e **Scalar UI**.
- Fornecer endpoints operacionais e diagnósticos semânticos envelopados no padrão `Result<T>`.

---

## 2. Endpoints Operacionais

### 2.1 Verificação de Saúde Operacional (`Health Check`)

- **Método:** `GET`
- **Rota:** `/api/v1/health`
- **Sumário OpenAPI:** `Verifica a saúde operacional da API do AdMetricsPro`
- **Autenticação:** Pública (não requer token JWT).

#### Payload de Entrada
Não possui corpo na requisição (`No Content`).

#### Payload de Retorno (Sucesso - HTTP 200 OK)
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "description": "",
    "type": 0
  },
  "value": {
    "status": "Healthy",
    "timestampUtc": "2026-09-03T13:33:00.0000000Z",
    "service": "AdMetricsPro API",
    "environment": "Development"
  }
}
```

#### Payload de Retorno (Falha de Negócio / Erro - HTTP 400/500)
Caso ocorra uma falha crítica de dependência (ex.: indisponibilidade total do MasterDb), a API retorna no envelope padronizado:
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Health.Degraded",
    "description": "Serviços essenciais estão indisponíveis.",
    "type": 0
  },
  "value": null
}
```

---

## 3. Especificação de Contratos & Ferramentas Interativas

### 3.1 Contrato OpenAPI v1
- **Endpoint:** `GET /openapi/v1.json`
- **Descrição:** Documento formal da especificação OpenAPI 3.1 gerado nativamente pelo ASP.NET Core 10 (`builder.Services.AddOpenApiDocumentation()`).
- **Campos do Schema:**
  - `info.title`: "AdMetricsPro API"
  - `info.version`: "v1"
  - `info.description`: "SaaS de Gestão Unificada de Tráfego Pago (Meta Ads, Google Ads, Bing Ads e TikTok Ads)."

### 3.2 Scalar API Reference
- **Endpoint:** `GET /scalar/v1`
- **Descrição:** Interface gráfica interativa moderna gerada pelo pacote `Scalar.AspNetCore`, permitindo a visualização dos contratos, teste interativo de endpoints e inspeção dos modelos de dados.
- **Ambientes:** Habilitada por padrão nos ambientes `Development` e `Staging`.

---

## 4. Governança e Regras de Implementação

1. **Envelope Result Obrigatório:** Nenhum endpoint deve expor exceções raw para o cliente. Toda resposta operacional ou transacional deve serializar `Result` ou `Result<T>`.
2. **Atributos OpenAPI Mandatórios:** Todos os endpoints em controladores devem conter:
   - `[EndpointSummary("...")]`
   - `[ProducesResponseType(typeof(Result<T>), StatusCodes.Status...)]`
3. **Padrão de Roteamento:** Todas as rotas são normalizadas em letras minúsculas (`options.LowercaseUrls = true`), iniciando com `/api/v1/`.
4. **Documentação XML Estrita:** Todo membro público do projeto deve possuir comentário XML de documentação legível, sob pena de erro de compilação (`TreatWarningsAsErrors=true`).
