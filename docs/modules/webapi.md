# Módulo WebApi: Host ASP.NET Core 10, OpenAPI & Scalar UI

## 1. Visão Geral
O projeto `src/Backend/WebApi` atua como o ponto de entrada (Host) HTTP para o ecossistema backend do **AdMetricsPro**. Ele é responsável por:
- Inicializar o runtime do ASP.NET Core 10 com roteamento centralizado (`LowercaseUrls = true`).
- Configurar os pipelines de middleware HTTP (roteamento, autenticação, autorização e tratamento de requisições).
- Expor a documentação viva e interativa de contratos via **OpenAPI v1** (`/openapi/v1.json`) e **Scalar UI** (`/scalar/v1`).
- Suportar autenticação corporativa unificada via JWT Bearer nos contratos OpenAPI e na interface do Scalar.
- Fornecer endpoints operacionais, administrativos e diagnósticos semânticos envelopados no padrão `Result<T>` ou `Result`.

---

## 2. Autenticação Corporativa & Segurança no OpenAPI

### 2.1 Esquema de Segurança Bearer JWT
A especificação OpenAPI v1 registra o componente de segurança corporativo:
- **Nome do Esquema:** `Bearer`
- **Tipo:** `http`
- **Scheme:** `bearer`
- **Formato:** `JWT`
- **Header:** `Authorization: Bearer {token}`
- **Escopo:** Requisito global na OpenAPI para sinalizar autenticação obrigatória em rotas administrativas (exceção para diagnósticos públicos como `/api/v1/health`).

### 2.2 Interface Interativa Scalar UI
- **Rota:** `GET /scalar/v1`
- **Ambientes Habilitados:** `Development` e `Staging`.
- **Tema Visual:** `ScalarTheme.Moon`.
- **Autenticação Interativa:** Esquema preferencial `Bearer` pré-configurado, permitindo que administradores cliquem em *Authorize* e informem seus tokens corporativos (ex.: tokens de SuperAdmin ou sessões de Shadow Mode/Impersonação emitidas pelo módulo Master).

---

## 3. Catálogo de Endpoints Administrativos & Operacionais

| Controlador | Rota Base | Finalidade | Status Codes Semânticos |
|---|---|---|---|
| `HealthController` | `/api/v1/health` | Diagnóstico de integridade operacional da API | 200 OK |
| `PlansController` | `/api/v1/plans` | Governança de planos comerciais, cotas e tiers | 200 OK, 201 Created, 400 BadRequest, 404 NotFound, 409 Conflict, 422 UnprocessableEntity |
| `TenantsController` | `/api/v1/tenants` | Gestão de inquilinos e emissão/revogação de Shadow Mode (Impersonação) | 200 OK, 400 BadRequest, 404 NotFound, 422 UnprocessableEntity |
| `ApiHealthController` | `/api/v1/admin/api-health` | Monitoramento de rate limits de mídia (80%), saúde de tokens e cotas | 200 OK, 400 BadRequest, 422 UnprocessableEntity |
| `BillingController` | `/api/v1/billing` | Régua de cobrança, suspensão progressiva e Dunning Engine | 200 OK, 400 BadRequest, 422 UnprocessableEntity |
| `FeatureFlagsController` | `/api/v1/admin/feature-flags` | Governança de flags dinâmicas, rollouts e Kill Switches de emergência | 200 OK, 201 Created, 400 BadRequest, 404 NotFound, 409 Conflict, 422 UnprocessableEntity |

---

## 4. Exemplos Estruturados de Contratos (Padrão Result)

### 4.1 Resposta com Envelope de Sucesso (`Result<T>`)
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
    "id": "c0000001-0000-0000-0000-000000000001",
    "name": "Enterprise Scale",
    "tier": "Enterprise",
    "monthlyPrice": 1490.0,
    "maxSeats": 50,
    "maxWorkspaces": 100,
    "monthlyAdSpendCap": 500000.0,
    "hasWhiteLabel": true,
    "hasAiCopilot": true
  }
}
```

### 4.2 Resposta com Envelope de Falha de Validação (`HTTP 422 Unprocessable Entity`)
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Plan.ValidationFailed",
    "description": "O teto de gastos de anúncios deve ser superior a zero para planos com IA ativa.",
    "type": 1
  },
  "value": null
}
```

### 4.3 Resposta com Envelope de Não Encontrado (`HTTP 404 Not Found`)
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "FeatureFlag.NotFound",
    "description": "A feature flag 'killswitch.automation.global' não foi localizada.",
    "type": 3
  },
  "value": null
}
```

---

## 5. Governança e Regras de Implementação

1. **Envelope Result Obrigatório:** Nenhum endpoint deve expor exceções raw para o cliente. Toda resposta operacional ou transacional deve serializar `Result` ou `Result<T>`.
2. **Atributos OpenAPI Mandatórios:** Todos os endpoints em controladores devem conter:
   - `[EndpointSummary("...")]`
   - `[ProducesResponseType(typeof(Result<T>), StatusCodes.Status...)]` contemplando os códigos HTTP semânticos (200/201, 400, 404, 409, 422).
3. **Padrão de Roteamento:** Todas as rotas são normalizadas em letras minúsculas (`options.LowercaseUrls = true`), iniciando com `/api/v1/`.
4. **Documentação XML Estrita:** Todo membro público do projeto deve possuir comentário XML de documentação legível, sob pena de erro de compilação (`TreatWarningsAsErrors=true`).
