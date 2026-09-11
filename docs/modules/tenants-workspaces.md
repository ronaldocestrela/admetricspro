# Módulo Tenants — Gestão de Workspaces (Clientes da Agência)

Este documento descreve as diretrizes, fluxos de persistência, validações de cotas e especificações técnicas de contratos e endpoints para a **Gestão de Workspaces** (clientes gerenciados da agência de tráfego) no contexto de banco de dados dedicado por inquilino (`TenantDbContext`).

---

## 1. Visão Geral e Arquitetura

No **AdMetricsPro**, uma agência cadastrada opera em um banco de dados dedicado (Database-per-Tenant) e gerencia múltiplos clientes finais, designados como **Workspaces**. 

Cada Workspace possui:
- Identificador único (`Id`).
- Nome comercial do cliente (`Name`).
- Documento fiscal do cliente sanitizado (`CnpjOrCpf`), validado matematicamente pelo algoritmo oficial brasileiro (CPF ou CNPJ).
- Orçamento mensal previsto para investimento em mídia paga (`MonthlyAdSpendBudget`).
- Segmento ou nicho de atuação (`Segment`).
- Status operacional (`IsActive`), permitindo pausar clientes sem apagar histórico.
- Timestamps de criação e atualização em UTC.

### 1.1 Isolamento de Dados e Validação de Cotas
- **Persistência Local do Tenant:** Toda a tabela `Workspaces` reside exclusivamente no `TenantDbContext` isolado do inquilino.
- **Validação Desacoplada de Cotas do Plano:** Para respeitar o limite de clientes ativos do plano contratado sem quebrar o desacoplamento de contextos, o módulo `Tenants.Application` despacha in-memory via `MediatR` a consulta `GetTenantPlanLimitsQuery(TenantId)`. O módulo `Master.Application` atende a consulta e responde com o DTO `TenantPlanLimitsDto`.

### 1.2 Cotas de Workspaces por Tier
| Tier Comercial | Cota Máxima de Workspaces Ativos |
| :--- | :--- |
| **Trial** | Até 3 workspaces |
| **Starter** | Até 3 workspaces |
| **Pro** | Até 15 workspaces |
| **Enterprise** | Ilimitado (`int.MaxValue`) |

---

## 2. Endpoints da Web API (`/api/v1/workspaces`)

Todos os endpoints operam sob resolução de contexto multitenant (cabeçalho `X-Tenant-Id`, subdomínio CNAME ou token JWT) e retornam envelopes no padrão `Result<T>`.

### 2.1 Cadastrar Novo Workspace
- **Rota:** `POST /api/v1/workspaces`
- **Sumário OpenAPI:** `Cadastra um novo cliente/workspace na agência com validação de cotas`
- **Cabeçalhos:** `X-Tenant-Id: {guid}` ou Bearer JWT
- **Códigos HTTP:** `201 Created`, `400 BadRequest`, `401 Unauthorized`, `409 Conflict`.

#### Payload de Entrada (`CreateWorkspaceApiRequest`):
```json
{
  "name": "Loja Virtual Exemplo LTDA",
  "cnpjOrCpf": "12.345.678/0001-95",
  "monthlyAdSpendBudget": 15000.00,
  "segment": "E-commerce de Moda"
}
```

#### Retorno de Sucesso (`201 Created`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "description": "",
    "type": 0
  },
  "value": "b5a9df7c-3f41-4796-a7dc-51b682669e4f"
}
```

#### Retorno de Cota Excedida (`400 BadRequest`):
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Workspace.QuotaExceeded",
    "description": "Limite de clientes/workspaces atingido para o plano atual (3). Faça upgrade do plano da agência para cadastrar mais clientes.",
    "type": 1
  }
}
```

#### Retorno de Conflito de Documento (`409 Conflict`):
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Workspace.CnpjOrCpfAlreadyExists",
    "description": "Já existe um cliente/workspace cadastrado com este CPF/CNPJ neste inquilino.",
    "type": 3
  }
}
```

---

### 2.2 Listar Workspaces da Agência
- **Rota:** `GET /api/v1/workspaces` (com filtro opcional `?activeOnly=true`)
- **Sumário OpenAPI:** `Lista os clientes/workspaces da agência`
- **Retorno:** `200 OK`

#### Retorno JSON (`Result<IReadOnlyList<WorkspaceDto>>`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": { "code": "", "description": "", "type": 0 },
  "value": [
    {
      "id": "b5a9df7c-3f41-4796-a7dc-51b682669e4f",
      "name": "Loja Virtual Exemplo LTDA",
      "cnpjOrCpf": "12345678000195",
      "monthlyAdSpendBudget": 15000.00,
      "segment": "E-commerce de Moda",
      "isActive": true,
      "createdAtUtc": "2026-09-07T14:00:00Z",
      "updatedAtUtc": null
    }
  ]
}
```

---

### 2.3 Obter Detalhes do Workspace
- **Rota:** `GET /api/v1/workspaces/{id}`
- **Retorno:** `200 OK` ou `404 NotFound`

---

### 2.4 Atualizar Cadastro do Workspace
- **Rota:** `PUT /api/v1/workspaces/{id}`
- **Payload de Entrada (`UpdateWorkspaceApiRequest`):**
```json
{
  "name": "Loja Virtual Exemplo Rebranded LTDA",
  "cnpjOrCpf": "12.345.678/0001-95",
  "monthlyAdSpendBudget": 25000.00,
  "segment": "E-commerce Omnichannel"
}
```
- **Retorno:** `200 OK`, `400 BadRequest`, `404 NotFound` ou `409 Conflict`.

---

### 2.5 Alternar Status Operacional (Ativar/Pausar)
- **Rota:** `PATCH /api/v1/workspaces/{id}/toggle-status`
- **Comportamento:**
  - Se estiver ativo, desativa imediatamente.
  - Se estiver inativo e for solicitado ativar, verifica a cota de workspaces do plano ativo. Caso a agência já tenha atingido o teto de ativos, recusa com erro `Workspace.QuotaExceeded`.
- **Retorno:** `200 OK`, `400 BadRequest` ou `404 NotFound`.

---

## 3. Catálogo de Erros de Negócio

| Código do Erro | Tipo (`ErrorType`) | Descrição |
| :--- | :--- | :--- |
| `Workspace.NameRequired` | Validation (1) | O nome do workspace é obrigatório. |
| `Workspace.NameTooLong` | Validation (1) | O nome do workspace não pode exceder 150 caracteres. |
| `Workspace.InvalidTaxDocument`| Validation (1) | O documento informado não é um CPF ou CNPJ válido. |
| `Workspace.InvalidBudget` | Validation (1) | O orçamento mensal de mídia não pode ser negativo. |
| `Workspace.SegmentTooLong` | Validation (1) | O segmento não pode exceder 100 caracteres. |
| `Workspace.QuotaExceeded` | Validation (1) | Cota máxima de workspaces ativos do plano atingida. |
| `Workspace.CnpjOrCpfAlreadyExists` | Conflict (3) | Já existe um cliente com este CPF/CNPJ cadastrado no inquilino. |
| `Workspace.NotFound` | NotFound (2) | Workspace com o identificador informado não foi localizado. |
| `Workspace.AlreadyActive` | Conflict (3) | O workspace já se encontra ativo. |
| `Workspace.AlreadyInactive` | Conflict (3) | O workspace já se encontra inativo. |

---

## 4. Frontend Blazor Server (Regra 9 do AGENTS.md)

Em conformidade estrita com o princípio de separação de camadas do `AGENTS.md`, o projeto `WebApp` opera estritamente no modo de apresentação, consumindo todos os dados e regras através da Web API via `HttpClient` fortemente tipado e tratando envelopes `Result<T>`.

### 4.1 Cliente HTTP Tipado (`IWorkspaceClientService`)
- `CreateWorkspaceAsync(CreateWorkspaceModel, CancellationToken)`: Despacha `POST /api/v1/workspaces`.
- `GetWorkspacesAsync(bool? activeOnly, CancellationToken)`: Despacha `GET /api/v1/workspaces`.
- `GetWorkspaceByIdAsync(Guid id, CancellationToken)`: Despacha `GET /api/v1/workspaces/{id}`.
- `UpdateWorkspaceAsync(Guid id, UpdateWorkspaceModel, CancellationToken)`: Despacha `PUT /api/v1/workspaces/{id}`.
- `ToggleWorkspaceStatusAsync(Guid id, CancellationToken)`: Despacha `PATCH /api/v1/workspaces/{id}/toggle-status`.

### 4.2 Telas e Componentes Blazor
1. **Página de Gestão de Clientes (`/workspaces` - `WorkspacesPage.razor`):**
   - Painel com cards estatísticos: Total de Clientes, Clientes Ativos e Verba Mensal Gerenciada.
   - Toolbar com pesquisa em tempo real e filtros de status (Todos, Ativos, Pausados).
   - Tabela responsiva com dados sanitizados, badge de documento fiscal, segmento e orçamento formatado.
   - Alternância de status operacional com liberação imediata de cota ou aviso de bloqueio caso a cota do plano esteja esgotada.
   - Modal unificado para cadastro e edição com validações pré-submissão e feedback visual de progresso.
2. **Modal Rápido FTUX (`WorkspaceQuickModal.razor`):**
   - Cadastro ágil do primeiro cliente durante o checklist de ativação da agência.

---

## 5. Suíte de Testes Automatizados (TDD Global)

A integridade do módulo é garantida por testes automatizados em três níveis:

1. **Testes Unitários de Domínio & Handlers (`UnitTests.Backend`):**
   - `WorkspaceTests.cs`: Validações de fábrica estática `Workspace.Create`, algoritmos oficiais de CPF/CNPJ (módulo 11), sanitização, invariantes de atualização (`UpdateDetails`), ativação e desativação.
   - `CreateWorkspaceCommandHandlerTests.cs`: Validação de cotas por plano via `MediatR` desacoplado, conflito de documento duplicado, persistência transacional com `IUnitOfWork`.
   - `UpdateWorkspaceCommandHandlerTests.cs` e `ToggleWorkspaceStatusCommandHandlerTests.cs`: Fluxos de alteração e controle de cota na reativação.
2. **Testes Unitários de Interface (`UnitTests.Frontend`):**
   - `WorkspacesPageTests.cs`: Testes bUnit cobrindo renderização com branding institucional, empty state, cards de resumo, tabela de clientes e interações de alternância de status.
3. **Testes de Aceitação de API (`AcceptanceTests`):**
   - `WorkspacesEndpointTests.cs`: Testes de integração via `WebApplicationFactory` cobrindo os códigos HTTP `201 Created`, `400 BadRequest` (cota excedida), `200 OK` e `404 NotFound`.
