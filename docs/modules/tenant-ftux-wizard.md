# Módulo Tenants — Wizard de Primeiro Acesso da Agência (FTUX — First-Time User Experience)

Este documento especifica a arquitetura, regras de negócio, contratos de API, estrutura de modais e fluxos de interação do **Wizard de Primeiro Acesso (FTUX Checklist)** da agência no **AdMetricsPro**.

---

## 1. Visão Geral e Objetivo

Quando o gestor ou proprietário (Owner) de uma nova agência conclui o cadastro de self-service onboarding e acessa o painel principal (`/dashboard`), ele é recepcionado por um checklist interativo e visual que o conduz pelos 4 passos essenciais para ativação de valor da plataforma.

### 1.1 Checklist Progressivo (0% a 100%)
O progresso é composto por 4 marcos de 25% cada:

| Passo | Marco | Percentual | Ação / Gatilho | Estado Inicial |
| :--- | :--- | :--- | :--- | :--- |
| **Passo 1** | **Ambiente Provisionado** | 25% | Banco de dados SQL Server dedicado e usuário administrador criados com sucesso. | Sempre `true` quando acessa o painel do inquilino. |
| **Passo 2** | **Primeiro Cliente / Workspace** | 25% | Cadastro do 1º cliente gerenciado da agência (`Workspaces` >= 1). | `WorkspaceQuickModal` integrado com validação de CNPJ/CPF e orçamentos. |
| **Passo 3** | **Primeira Conta de Anúncios** | 25% | Conexão de conta de mídia (Meta, Google, Bing, TikTok) ou carregamento de **Dados Demonstrativos**. | `AdConnectionQuickModal` com modo Demo (`IsDemo = true`) para simulação instantânea de métricas. |
| **Passo 4** | **Convidar Time ou Criar Squad** | 25% | Convite enviado a um membro do time (`TenantUsers` > 1) OU criação do 1º Squad interno (`Squads` >= 1). | `TeamQuickModal` permitindo convite por e-mail com perfil RBAC ou criação de squad de atendimento. |

Ao atingir 100%, o componente exibe uma faixa comemorativa com botão de dispensa ("Entendido, ocultar checklist"), que salva no `localStorage` a preferência de fechamento do card.

---

## 2. Endpoints da Web API

### 2.1 Consulta de Status do FTUX
- **Rota:** `GET /api/v1/tenants/ftux-status`
- **Sumário:** `Obtém o progresso e estado de conclusão dos 4 passos de onboarding FTUX do tenant.`
- **Cabeçalhos:** `X-Tenant-Id: {guid}` ou Bearer JWT
- **Códigos HTTP:** `200 OK`, `401 Unauthorized`

#### Resposta de Sucesso (`200 OK`):
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
    "hasDatabaseProvisioned": true,
    "hasWorkspace": true,
    "hasConnectedAdAccount": true,
    "hasTeamOrSquad": false,
    "completionPercentage": 75,
    "isCompleted": false,
    "totalWorkspaces": 1,
    "totalConnectedAccounts": 1,
    "totalUsers": 1,
    "totalSquads": 0
  }
}
```

---

### 2.2 Conexão de Conta de Anúncios Demonstrativa (Modo Demo)
- **Rota:** `POST /api/v1/integrations/demo-account`
- **Sumário:** `Conecta uma conta de anúncios demonstrativa em um workspace para acelerar o FTUX do tenant.`
- **Cabeçalhos:** `X-Tenant-Id: {guid}`
- **Códigos HTTP:** `201 Created`, `400 BadRequest`, `401 Unauthorized`, `404 NotFound`

#### Payload de Entrada (`ConnectDemoAccountApiRequest`):
```json
{
  "workspaceId": "d826a421-4ec4-49c7-9b2f-3ea1bb72d245",
  "platform": "MetaAds",
  "accountName": "Conta Demo Meta (E-commerce Alpha)"
}
```

#### Resposta de Sucesso (`201 Created`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "description": "",
    "type": 0
  },
  "value": "7b8f9e61-a142-4f39-8588-e9fcae127391"
}
```

---

### 2.3 Listagem de Contas Conectadas
- **Rota:** `GET /api/v1/integrations/connected-accounts`
- **Sumário:** `Obtém a lista de contas de anúncios conectadas ao tenant operacional.`
- **Parâmetros Opcionais de Query:** `?workspaceId={guid}`
- **Códigos HTTP:** `200 OK`, `401 Unauthorized`

#### Resposta de Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "description": "",
    "type": 0
  },
  "value": [
    {
      "id": "7b8f9e61-a142-4f39-8588-e9fcae127391",
      "workspaceId": "d826a421-4ec4-49c7-9b2f-3ea1bb72d245",
      "platform": "MetaAds",
      "externalAccountId": "act_demo_37194",
      "accountName": "Conta Demo Meta (E-commerce Alpha)",
      "currency": "BRL",
      "status": "Connected",
      "isDemo": true,
      "createdAtUtc": "2026-09-07T14:30:00Z"
    }
  ]
}
```

---

### 2.4 Convite de Membro da Equipe
- **Rota:** `POST /api/v1/tenants/users`
- **Sumário:** `Cadastra ou convida um novo membro da equipe com perfil RBAC no tenant operacional.`
- **Cabeçalhos:** `X-Tenant-Id: {guid}`
- **Códigos HTTP:** `201 Created`, `400 BadRequest`, `401 Unauthorized`, `409 Conflict`

#### Payload de Entrada (`InviteTenantUserApiRequest`):
```json
{
  "fullName": "Carlos Gestor de Tráfego",
  "email": "carlos@agenciaalpha.com.br",
  "phoneNumber": "11988887777",
  "role": 2
}
```
*(Papéis RBAC suportados: `Owner = 0`, `Admin = 1`, `Manager = 2`, `Analyst = 3`, `ClientViewer = 4`)*

#### Resposta de Sucesso (`201 Created`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": "",
    "description": "",
    "type": 0
  },
  "value": "5d2b781a-6539-4ce1-85f9-2d1796be20a1"
}
```

---

## 3. Componentes Blazor Server no Frontend

Em conformidade rigorosa com o **Princípio 9 do AGENTS.md**, os componentes frontend operam no modo apresentação interativa, sem referências a camadas de persistência, consumindo as rotas Web API através dos serviços HTTP tipados:
- `ITenantFtuxClientService` / `TenantFtuxClientService`
- `IWorkspaceClientService` / `WorkspaceClientService`
- `ITenantTeamClientService` / `TenantTeamClientService`

### 3.1 `AgencyFtuxChecklist.razor`
- **Localização:** `src/Frontend/WebApp/Components/Dashboard/AgencyFtuxChecklist.razor`
- **Responsabilidade:** Renderiza a barra de progresso e os cards acionáveis para cada um dos 4 passos. Ao clicar nas ações de cada passo, dispara os eventos que abrem os respectivos modais rápidos.
- **Persistência de Visibilidade:** Utiliza JavaScript Interop / LocalStorage (`agency_ftux_dismissed_{tenantId}`) para não reaparecer após dispensa voluntária pelo usuário quando atinge 100%.

### 3.2 Modais Rápidos
1. `WorkspaceQuickModal.razor`: Criação expressa do primeiro cliente gerenciado com máscaras de CPF/CNPJ e orçamento mensal de mídia.
2. `AdConnectionQuickModal.razor`: Conexão rápida de plataformas Meta, Google, Bing e TikTok, com botão de destaque **"Carregar Dados Demonstrativos (Modo Demo)"**.
3. `TeamQuickModal.razor`: Alternância em abas para convidar usuário por e-mail ou criar o primeiro Squad de atendimento da agência.
